using Dalamud.Game.Network;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Network.Universalis.MarketBoardUploaders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AsyncAwaitBestPractices;
using Polly;

namespace UniversalisCommon
{
    public class PacketProcessor
    {
        private const int MessageHeaderSize = 0x20;

        private readonly List<MarketBoardItemRequest> _marketBoardRequests = new List<MarketBoardItemRequest>();
        private readonly IMarketBoardUploader _uploader;

        private IDictionary<short, Func<byte[], bool>> _packetHandlers;

        public uint CurrentWorldId { get; set; }
        public string UploaderId { get; }

        public EventHandler<string> Log;

        public PacketProcessor(string apiKey)
        {
            _uploader = new UniversalisMarketBoardUploader(this, apiKey);
            UploaderId = Guid.NewGuid().ToString().Replace("-", "");

            Initialize();
        }

        private void Initialize()
        {
            var policy = Policy
                .Handle<WebException>()
                .WaitAndRetryForeverAsync(
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, retryCount, _) =>
                    {
                        Log?.Invoke(this,
                            $"[WARN] Failed to fetch opcode definitions (attempt #{retryCount}); retrying...\n{exception.Message}");
                    });
            policy
                .ExecuteAsync(() => Task.Run(() =>
                {
                    var definitions = Definitions.Get();
                    _packetHandlers = new Dictionary<short, Func<byte[], bool>>
                    {
                        { definitions.PlayerSpawn, ProcessPlayerSpawn },
                        { definitions.MarketBoardItemRequestStart, ProcessMarketBoardItemRequestStart },
                        { definitions.MarketBoardOfferings, ProcessMarketBoardOfferings },
                        { definitions.MarketBoardHistory, ProcessMarketBoardHistory },
                        { definitions.MarketTaxRates, ProcessMarketTaxRates },
                        { definitions.ContentIdNameMapResp, ProcessContentIdNameMapResp },
                    };
                }))
                .SafeFireAndForget(
                    continueOnCapturedContext: true,
                    onException: ex => Log?.Invoke(this, $"[ERROR] Could not fetch opcode definitions:\n{ex}"));
        }

        /// <summary>
        /// Process a zone proto message and scan for relevant data.
        /// </summary>
        /// <param name="message">The message bytes</param>
        /// <returns>True if an upload succeeded</returns>
        public bool ProcessZonePacket(byte[] message)
        {
            if (_packetHandlers == null)
            {
                return false;
            }

            if (message.Length < MessageHeaderSize)
            {
                return false;
            }

            var opcode = BitConverter.ToInt16(message, 0x12);
            return _packetHandlers.TryGetValue(opcode, out var handler) && handler(message);
        }

        private bool ProcessContentIdNameMapResp(byte[] message)
        {
            var cid = BitConverter.ToUInt64(message, 0x20);
            var name = Encoding.UTF8.GetString(message, 0x28, 32).TrimEnd('\u0000');

            try
            {
                _uploader.UploadCrafterName(cid, name);
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke(this, "[ERROR] Content ID upload failed:\n" + ex);
                return false;
            }
        }

        private bool ProcessMarketTaxRates(byte[] message)
        {
            var taxRates = MarketTaxRates.Read(message.Skip(0x20).ToArray());
            if (taxRates.Category != 0xb0009)
            {
                return false;
            }

            var request = new UniversalisTaxDataUploadRequest
            {
                UploaderId = UploaderId,
                WorldId = CurrentWorldId,
                TaxData = new UniversalisTaxData
                {
                    LimsaLominsa = taxRates.LimsaLominsaTax,
                    Uldah = taxRates.UldahTax,
                    Gridania = taxRates.GridaniaTax,
                    Ishgard = taxRates.IshgardTax,
                    Kugane = taxRates.KuganeTax,
                    Crystarium = taxRates.CrystariumTax,
                    Sharlayan = taxRates.SharlayanTax,
                    Tuliyollal = taxRates.TuliyollalTax,
                },
            };

            Log?.Invoke(this, $"Uploading tax rates {JsonConvert.SerializeObject(taxRates)}");
            try
            {
                _uploader.UploadTaxRates(request);
                Log?.Invoke(this, "Tax rates upload completed.");
                return true;
            }
            catch (Exception ex)
            {
                Log?.Invoke(this, "[ERROR] Tax rates upload failed:\n" + ex);
                return false;
            }
        }

        private bool ProcessMarketBoardHistory(byte[] message)
        {
            var history = MarketBoardHistory.Read(message.Skip(0x20).ToArray());
            var itemId = history.CatalogId;

            var request = _marketBoardRequests.LastOrDefault(r => r.IsNew || r.CatalogId == itemId && !r.IsDone);
            if (request == null)
            {
                Log?.Invoke(this,
                    $"[ERROR] Market Board history arrived without a corresponding request: item#{itemId}");
                return false;
            }

            // Detect the request's item ID from the history packet
            request.CatalogId = itemId;

            if (request.ListingsRequestId != -1)
            {
                Log?.Invoke(this,
                    $"[ERROR] Market Board data history sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                return false;
            }

            request.History.AddRange(history.HistoryListings);
            request.HistoryReceived = true;

            Log?.Invoke(this, $"Added history for item#{history.CatalogId}");

            return request.IsDone && SendCurrentRequest(request);
        }

        private bool ProcessMarketBoardOfferings(byte[] message)
        {
            var listings = MarketBoardCurrentOfferings.Read(message.Skip(0x20).ToArray());
            var itemId = listings.ItemListings[0].CatalogId;

            var request = _marketBoardRequests.LastOrDefault(r => r.IsNew || r.CatalogId == itemId && !r.IsDone);
            if (request == null)
            {
                Log?.Invoke(this,
                    $"[ERROR] Market Board listings arrived without a corresponding request: item#{itemId}");
                return false;
            }

            // Detect the request's item ID from the first listing
            request.CatalogId = itemId;

            if (request.Listings.Count + listings.ItemListings.Count > request.AmountToArrive)
            {
                Log?.Invoke(this,
                    $"[ERROR] Too many Market Board listings received for request: {request.Listings.Count + listings.ItemListings.Count} > {request.AmountToArrive} item#{listings.ItemListings[0].CatalogId}");
                return false;
            }

            if (request.ListingsRequestId != -1 && request.ListingsRequestId != listings.RequestId)
            {
                Log?.Invoke(this,
                    $"[ERROR] Non-matching RequestIds for Market Board data request: {request.ListingsRequestId}, {listings.RequestId}");
                return false;
            }

            if (request.ListingsRequestId == -1 && request.Listings.Count > 0)
            {
                Log?.Invoke(this,
                    $"[ERROR] Market Board data request sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                return false;
            }

            if (request.ListingsRequestId == -1)
            {
                request.ListingsRequestId = listings.RequestId;
                Log?.Invoke(this, $"First Market Board packet in sequence: {listings.RequestId}");
            }

            request.Listings.AddRange(listings.ItemListings);

            Log?.Invoke(this,
                $"Added {listings.ItemListings.Count} ItemListings to request#{request.ListingsRequestId}, now {request.Listings.Count}/{request.AmountToArrive}, item#{request.CatalogId}");

            return request.IsDone && SendCurrentRequest(request);
        }

        private bool SendCurrentRequest(MarketBoardItemRequest request)
        {
            if (CurrentWorldId == 0)
            {
                Log?.Invoke(this,
                    "[ERROR] Not sure about your current world. Please move your character between zones once to start uploading.");
                return false;
            }

            Log?.Invoke(this,
                $"Market Board request finished, starting upload: request#{request.ListingsRequestId} item#{request.CatalogId} amount#{request.AmountToArrive}");
            try
            {
                _uploader.Upload(request);
                Log?.Invoke(this, "Market Board data upload completed.");
                return true;
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse res && (int)res.StatusCode >= 500)
            {
                Log?.Invoke(this, $"[ERROR] Market Board data upload failed due to a server error:\n{ex.Message}");
            }
            catch (WebException ex) when (ex.Response is HttpWebResponse res && (int)res.StatusCode >= 400 &&
                                          (int)res.StatusCode < 500)
            {
                Log?.Invoke(this, $"[ERROR] Market Board data upload failed due to a client error:\n{ex.Message}");
            }
            catch (WebException ex)
            {
                Log?.Invoke(this, $"[ERROR] Market Board data upload failed:\n{ex.Message}");
            }
            catch (Exception ex)
            {
                Log?.Invoke(this, $"[ERROR] Market Board data upload failed:\n{ex}");
            }

            return false;
        }

        private bool ProcessMarketBoardItemRequestStart(byte[] message)
        {
            var status = BitConverter.ToUInt32(message, 0x20);
            if (status == 0x70000003)
            {
                Log?.Invoke(this, "Client is currently rate limited by the game server.");
                return false;
            }

            var amount = BitConverter.ToInt32(message, 0x24);
            _marketBoardRequests.Add(new MarketBoardItemRequest
            {
                AmountToArrive = amount,
                Listings = new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                History = new List<MarketBoardHistory.MarketBoardHistoryListing>(),
            });

            Log?.Invoke(this, $"NEW MB REQUEST START: Expecting {amount} listings");
            return false;
        }

        private bool ProcessPlayerSpawn(byte[] message)
        {
            var worldId = BitConverter.ToUInt16(message, 0x34);
            if (worldId != CurrentWorldId)
            {
                CurrentWorldId = worldId;
                Log?.Invoke(this, $"Current world detected, set to world#{worldId}");
            }

            return false;
        }
    }
}