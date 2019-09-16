using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.Network;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.Structures;
using Dalamud.Game.Network.Universalis.MarketBoardUploaders;
using UniversalisPlugin;

namespace UniversalisCommon
{
    public class PacketProcessor
    {
        private readonly Definitions _definitions;

        private readonly List<MarketBoardItemRequest> _marketBoardRequests = new List<MarketBoardItemRequest>();
        private readonly IMarketBoardUploader _uploader;

        public uint CurrentWorldId { get; set; }
        public ulong LocalContentId { get; set; }

        public EventHandler<string> Log;

        public PacketProcessor(string apiKey)
        {
            _definitions = Definitions.Get();
            _uploader = new UniversalisMarketBoardUploader(this, apiKey);
        }

        /// <summary>
        /// Process a zone proto message and scan for relevant data.
        /// </summary>
        /// <param name="message">The message bytes</param>
        /// <returns>True if an upload succeeded</returns>
        public bool ProcessZonePacket(byte[] message)
        {
            var opCode = BitConverter.ToInt16(message, 0x12);

            if (opCode == _definitions.PlayerSpawn)
            {
                CurrentWorldId = BitConverter.ToUInt16(message, 0x24);

                return false;
            }

            if (opCode == _definitions.PlayerSetup)
            {
                LocalContentId = BitConverter.ToUInt64(message, 0x20);
                Log?.Invoke(this, $"New CID: {LocalContentId.ToString("X")}");
                return false;
            }

            if (opCode == _definitions.MarketBoardItemRequestStart)
            {
                var catalogId = (uint) BitConverter.ToInt32(message, 0x20);
                var amount = message[0x2B];

                _marketBoardRequests.Add(new MarketBoardItemRequest
                {
                    CatalogId = catalogId,
                    AmountToArrive = amount,
                    Listings = new List<MarketBoardCurrentOfferings.MarketBoardItemListing>(),
                    History = new List<MarketBoardHistory.MarketBoardHistoryListing>()
                });

                Log?.Invoke(this, $"NEW MB REQUEST START: item#{catalogId} amount#{amount}");
                return false;
            }

            if (opCode == _definitions.MarketBoardOfferings)
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
                        Log?.Invoke(this, "[ERROR] Not sure about your current world. Please move your character between zones once to start uploading.");
                        return false;
                    }

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

            if (opCode == _definitions.MarketBoardHistory)
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

            if ( opCode == _definitions.ContentIdNameMapResp)
            {
                var cid = BitConverter.ToUInt64(message, 0x20);
                var name = Encoding.UTF8.GetString(message, 0x28, 32).TrimEnd(new []{'\u0000'});

                try
                {
                    _uploader.UploadCrafterName(cid, name);
                    return true;
                }
                catch (Exception ex)
                {
                    Log?.Invoke(this, "[ERROR] Content ID upload failed:\n" + ex);
                }
            }

            return false;
        }
    }
}