using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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
        private const string UserAgent = "universalis_uploader";
        private const int ReadTimeoutMs = 20000;

        // sellerID and creatorID arrive null and will not convert to ulong.
        private static readonly JsonSerializer ListingSerializer = JsonSerializer.Create(
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

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

            var uploadRequest = new UniversalisMarketBoardUploadRequest
            {
                WorldId = (int)_packetProcessor.CurrentWorldId,
                UploaderId = uploader,
                ItemId = request.CatalogId,
                Listings = new List<UniversalisItemListingsEntry>(),
                Entries = new List<UniversalisHistoryEntry>(),
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

                uploadRequest.Listings.Add(universalisListing);
            }

            foreach (var marketBoardHistoryListing in request.History)
                uploadRequest.Entries.Add(new UniversalisHistoryEntry
                {
                    BuyerName = marketBoardHistoryListing.BuyerName,
                    Hq = marketBoardHistoryListing.IsHq,
                    OnMannequin = marketBoardHistoryListing.OnMannequin,
                    PricePerUnit = marketBoardHistoryListing.SalePrice,
                    Quantity = marketBoardHistoryListing.Quantity,
                    Timestamp = ((DateTimeOffset)marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds(),
                });

            if (_packetProcessor.CurrentRetainerId is ulong retainerId)
            {
                RestoreHiddenListings(uploadRequest, retainerId);
            }

            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            SubmitData(client, uploadRequest);

            _packetProcessor.Log?.Invoke(this,
                $"Universalis data upload for item#{request.CatalogId} to world#{uploadRequest.WorldId} completed.");
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
            var requestStr = JsonConvert.SerializeObject(data);
            Trace.WriteLine(requestStr);
            client.UploadString(ApiBase + $"/upload/{_apiKey}", "POST", requestStr);
        }

        /// <summary>
        /// A summoned retainer's listings are withheld from the price comparison window,
        /// so a snapshot taken there reads as a removal for listings that are still live.
        /// Restores them from Universalis' own rows: the game never tells a client what
        /// listing IDs its listings were assigned, and a reconstructed ID would post as
        /// an add and a remove rather than as a no-op.
        /// </summary>
        private void RestoreHiddenListings(UniversalisMarketBoardUploadRequest uploadRequest, ulong retainerId)
        {
            JToken listings;
            try
            {
                // DownloadString otherwise decodes as ANSI and mangles retainer names.
                using var client = new TimedWebClient { Encoding = Encoding.UTF8 };
                client.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
                var json = client.DownloadString(
                    $"{ApiBase}/api/v2/{uploadRequest.WorldId}/{uploadRequest.ItemId}" +
                    "?entries=0&statsWithin=0&fields=listings");
                listings = JObject.Parse(json)["listings"];
            }
            catch (Exception ex)
            {
                _packetProcessor.Log?.Invoke(this,
                    $"[WARN] Could not fetch listings for item#{uploadRequest.ItemId}; uploading unmodified:\n{ex.Message}");
                return;
            }

            if (listings == null)
            {
                _packetProcessor.Log?.Invoke(this,
                    $"[WARN] Listings response for item#{uploadRequest.ItemId} had no listings key; uploading unmodified.");
                return;
            }

            var present = new HashSet<ulong>(uploadRequest.Listings.Select(l => l.ListingId));
            var hidden = listings.ToObject<List<UniversalisItemListingsEntry>>(ListingSerializer)
                .Where(l => l.RetainerId == retainerId && !present.Contains(l.ListingId))
                .ToList();
            if (hidden.Count == 0)
            {
                return;
            }

            uploadRequest.Listings.AddRange(hidden);
            _packetProcessor.Log?.Invoke(this,
                $"Restored {hidden.Count} hidden listing(s) for retainer#{retainerId} on item#{uploadRequest.ItemId}.");
        }

        /// <summary>
        /// WebClient has no timeout property and defaults to 100 seconds.
        /// </summary>
        private class TimedWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = base.GetWebRequest(address);
                if (request != null)
                {
                    request.Timeout = ReadTimeoutMs;
                }

                return request;
            }
        }
    }
}