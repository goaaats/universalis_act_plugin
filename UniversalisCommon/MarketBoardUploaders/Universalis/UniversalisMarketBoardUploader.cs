using System;
using System.Collections.Generic;
using System.Net;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using UniversalisCommon;

namespace Dalamud.Game.Network.Universalis.MarketBoardUploaders
{
    internal class UniversalisMarketBoardUploader : IMarketBoardUploader
    {
        private const string ApiBase = "https://universalis.app";

        private readonly PacketProcessor _packetProcessor;
        private readonly string _apiKey;

        public UniversalisMarketBoardUploader(PacketProcessor packetProcessor, string apiKey)
        {
            _packetProcessor = packetProcessor;
            _apiKey = apiKey;
        }

        public void Upload(MarketBoardItemRequest request)
        {
            using var client = new WebClient();

            _packetProcessor.Log?.Invoke(this, "Starting Universalis upload.");
            var uploader = _packetProcessor.UploaderId;

            var listingsRequestObject = new UniversalisItemListingsUploadRequest
            {
                WorldId = (int)_packetProcessor.CurrentWorldId,
                UploaderId = uploader,
                ItemId = request.CatalogId,
                Listings = new List<UniversalisItemListingsEntry>(),
            };

            foreach (var marketBoardItemListing in request.Listings)
            {
                var universalisListing = new UniversalisItemListingsEntry
                {
                    ListingId = marketBoardItemListing.ListingId,
                    Hq = marketBoardItemListing.IsHq,
                    SellerId = marketBoardItemListing.RetainerOwnerId,
                    RetainerName = marketBoardItemListing.RetainerName,
                    RetainerId = marketBoardItemListing.RetainerId,
                    CreatorId = marketBoardItemListing.ArtisanId,
                    CreatorName = marketBoardItemListing.PlayerName,
                    OnMannequin = marketBoardItemListing.OnMannequin,
                    LastReviewTime = ((DateTimeOffset)marketBoardItemListing.LastReviewTime).ToUnixTimeSeconds(),
                    PricePerUnit = marketBoardItemListing.PricePerUnit,
                    Quantity = marketBoardItemListing.ItemQuantity,
                    RetainerCity = marketBoardItemListing.RetainerCityId,
                    Materia = new List<UniversalisItemMateria>(),
                };

                foreach (var itemMateria in marketBoardItemListing.Materia)
                    universalisListing.Materia.Add(new UniversalisItemMateria
                    {
                        MateriaId = itemMateria.MateriaId,
                        SlotId = itemMateria.Index,
                    });

                listingsRequestObject.Listings.Add(universalisListing);
            }

            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            SubmitData(client, listingsRequestObject);

            var historyRequestObject = new UniversalisHistoryUploadRequest
            {
                WorldId = (int)_packetProcessor.CurrentWorldId,
                UploaderId = uploader,
                ItemId = request.CatalogId,
                Entries = new List<UniversalisHistoryEntry>(),
            };

            foreach (var marketBoardHistoryListing in request.History)
                historyRequestObject.Entries.Add(new UniversalisHistoryEntry
                {
                    BuyerName = marketBoardHistoryListing.BuyerName,
                    Hq = marketBoardHistoryListing.IsHq,
                    OnMannequin = marketBoardHistoryListing.OnMannequin,
                    PricePerUnit = marketBoardHistoryListing.SalePrice,
                    Quantity = marketBoardHistoryListing.Quantity,
                    Timestamp = ((DateTimeOffset)marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds(),
                });

            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            SubmitData(client, historyRequestObject);

            _packetProcessor.Log?.Invoke(this,
                $"Universalis data upload for item#{request.CatalogId} to world#{historyRequestObject.WorldId} completed.");
        }

        public void UploadTaxRates(UniversalisTaxDataUploadRequest taxRatesRequest)
        {
            using var client = new WebClient();
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            SubmitData(client, taxRatesRequest);
        }

        public void UploadCrafterName(ulong contentId, string name)
        {
            using var client = new WebClient();
            dynamic crafterNameObj = new JObject();

            crafterNameObj.uploaderID = _packetProcessor.UploaderId;
            crafterNameObj.contentID = contentId;
            crafterNameObj.characterName = name;

            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            SubmitData(client, crafterNameObj);
        }

        private void SubmitData<T>(WebClient client, T data)
        {
            var remoteVersionStr = Policy
                .Handle<WebException>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(1))
                .ExecuteAndCapture(() => UploadData(client, data));
            if (remoteVersionStr.Outcome == OutcomeType.Failure)
            {
                throw remoteVersionStr.FinalException;
            }
        }

        private void UploadData<T>(WebClient client, T data)
        {
            client.UploadString(ApiBase + $"/upload/{_apiKey}", "POST", JsonConvert.SerializeObject(data));
        }
    }
}