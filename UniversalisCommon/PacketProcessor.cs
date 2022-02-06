using Dalamud.Game.Network;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Network.Universalis.MarketBoardUploaders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UniversalisCommon
{
    public class PacketProcessor
    {
        private readonly List<MarketBoardItemRequest> _marketBoardRequests = new List<MarketBoardItemRequest>();
        private readonly IMarketBoardUploader _uploader;

        private readonly IDictionary<short, Func<byte[], bool>> _packetHandlers;

        public uint CurrentWorldId { get; set; }
        public ulong LocalContentId { get; set; }

        public EventHandler<string> Log;
        public EventHandler<ulong> LocalContentIdUpdated;
        public EventHandler RequestContentIdUpdate;

        public PacketProcessor(string apiKey)
        {
            _uploader = new UniversalisMarketBoardUploader(this, apiKey);

            var definitions = Definitions.Get();
            _packetHandlers = new Dictionary<short, Func<byte[], bool>>
            {
                { definitions.PlayerSpawn, ProcessPlayerSpawn },
                { definitions.PlayerSetup, ProcessPlayerSetup },
                { definitions.MarketBoardItemRequestStart, ProcessMarketBoardItemRequestStart },
                { definitions.MarketBoardOfferings, ProcessMarketBoardOfferings },
                { definitions.MarketBoardHistory, ProcessMarketBoardHistory },
                { definitions.MarketTaxRates, ProcessMarketTaxRates },
                { definitions.ContentIdNameMapResp, ProcessContentIdNameMapResp },
            };
        }

        /// <summary>
        /// Process a zone proto message and scan for relevant data.
        /// </summary>
        /// <param name="message">The message bytes</param>
        /// <returns>True if an upload succeeded</returns>
        public bool ProcessZonePacket(byte[] message)
        {
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
            var request = new UniversalisTaxDataUploadRequest
            {
                UploaderId = LocalContentId.ToString("X"),
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
            var listing = MarketBoardHistory.Read(message.Skip(0x20).ToArray());

            var request = _marketBoardRequests.LastOrDefault(r => r.CatalogId == listing.CatalogId);

            if (request == null)
            {
                Log?.Invoke(this,
                    $"[ERROR] Market Board data arrived without a corresponding request: item#{listing.CatalogId}");
                return false;
            }

            if (request.ListingsRequestId != -1)
            {
                Log?.Invoke(this,
                    $"[ERROR] Market Board data history sequence break: {request.ListingsRequestId}, {request.Listings.Count}");
                return false;
            }

            request.History.AddRange(listing.HistoryListings);

            Log?.Invoke(this, $"Added history for item#{listing.CatalogId}");
            return false;
        }

        private bool ProcessMarketBoardOfferings(byte[] message)
        {
            var listing = MarketBoardCurrentOfferings.Read(message.Skip(0x20).ToArray());

            var request =
                _marketBoardRequests.LastOrDefault(
                    r => r.CatalogId == listing.ItemListings[0].CatalogId && !r.IsDone);

            if (request == null)
            {
                Log?.Invoke(this,
                    $"[ERROR] Market Board data arrived without a corresponding request: item#{listing.ItemListings[0].CatalogId}");
                return false;
            }

            if (request.Listings.Count + listing.ItemListings.Count > request.AmountToArrive)
            {
                Log?.Invoke(this,
                    $"[ERROR] Too many Market Board listings received for request: {request.Listings.Count + listing.ItemListings.Count} > {request.AmountToArrive} item#{listing.ItemListings[0].CatalogId}");
                return false;
            }

            if (request.ListingsRequestId != -1 && request.ListingsRequestId != listing.RequestId)
            {
                Log?.Invoke(this,
                    $"[ERROR] Non-matching RequestIds for Market Board data request: {request.ListingsRequestId}, {listing.RequestId}");
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
                request.ListingsRequestId = listing.RequestId;
                Log?.Invoke(this, $"First Market Board packet in sequence: {listing.RequestId}");
            }

            request.Listings.AddRange(listing.ItemListings);

            Log?.Invoke(this,
                $"Added {listing.ItemListings.Count} ItemListings to request#{request.ListingsRequestId}, now {request.Listings.Count}/{request.AmountToArrive}, item#{request.CatalogId}");

            if (request.IsDone)
            {
                if (CurrentWorldId == 0)
                {
                    Log?.Invoke(this,
                        "[ERROR] Not sure about your current world. Please move your character between zones once to start uploading.");
                    return false;
                }

                RequestContentIdUpdate?.Invoke(this, null);

                if (LocalContentId == 0)
                {
                    Log?.Invoke(this,
                        "[ERROR] Not sure about your character information. Please log in once with your character while having the program open to verify it.");
                    //return false;
                }

                LocalContentIdUpdated?.Invoke(this, LocalContentId);

                Log?.Invoke(this,
                    $"Market Board request finished, starting upload: request#{request.ListingsRequestId} item#{request.CatalogId} amount#{request.AmountToArrive}");
                try
                {
                    _uploader.Upload(request);
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.Invoke(this, "[ERROR] Market Board data upload failed:\n" + ex);
                }
            }

            return false;
        }

        private bool ProcessMarketBoardItemRequestStart(byte[] message)
        {
            var catalogId = (uint)BitConverter.ToInt32(message, 0x20);
            var amount = message[0x2B];

            _marketBoardRequests.Add(new MarketBoardItemRequest
            {
                CatalogId = catalogId,
                AmountToArrive = amount,
                Listings = new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                History = new List<MarketBoardHistory.MarketBoardHistoryListing>(),
            });

            Log?.Invoke(this, $"NEW MB REQUEST START: item#{catalogId} amount#{amount}");
            return false;
        }

        private bool ProcessPlayerSetup(byte[] message)
        {
            LocalContentId = BitConverter.ToUInt64(message, 0x20);
            Log?.Invoke(this, $"New CID: {LocalContentId:X}");
            LocalContentIdUpdated?.Invoke(this, LocalContentId);
            return false;
        }

        private bool ProcessPlayerSpawn(byte[] message)
        {
            CurrentWorldId = BitConverter.ToUInt16(message, 0x24);

            return false;
        }
    }
}