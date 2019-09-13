using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Newtonsoft.Json;
using UniversalisPlugin;

namespace Dalamud.Game.Network.Universalis.MarketBoardUploaders
{
    class UniversalisMarketBoardUploader : IMarketBoardUploader {
        private const string ApiBase = "https://universalis.app";
        //private const string ApiBase = "https://127.0.0.1:443";
        private const string ApiKey = "CiAQfpfIK6eDcBLRUSv1rp6neR7MsWsRkrhHvzBH";

        private UniversalisPlugin.UniversalisPluginControl dalamud;

        public UniversalisMarketBoardUploader(UniversalisPlugin.UniversalisPluginControl dalamud) {
            this.dalamud = dalamud;
        }

        public void Upload(MarketBoardItemRequest request) {
            using (var client = new WebClient()) {
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                this.dalamud.Log("Starting Universalis upload.");
                var uploader = this.dalamud.LocalContentId;

                var listingsRequestObject = new UniversalisItemListingsUploadRequest();
                listingsRequestObject.WorldId = (int) this.dalamud.CurrentWorldId;
                listingsRequestObject.UploaderId = uploader;
                listingsRequestObject.ItemId = request.CatalogId;

                listingsRequestObject.Listings = new List<UniversalisItemListingsEntry>();
                foreach (var marketBoardItemListing in request.Listings) {
                    var universalisListing = new UniversalisItemListingsEntry {
                        Hq = marketBoardItemListing.IsHq,
                        SellerId = marketBoardItemListing.RetainerOwnerId,
                        RetainerName = marketBoardItemListing.RetainerName,
                        RetainerId = marketBoardItemListing.RetainerId,
                        CreatorId = marketBoardItemListing.ArtisanId,
                        CreatorName = marketBoardItemListing.PlayerName,
                        OnMannequin = marketBoardItemListing.OnMannequin,
                        LastReviewTime = ((DateTimeOffset) marketBoardItemListing.LastReviewTime).ToUnixTimeSeconds(),
                        PricePerUnit = marketBoardItemListing.PricePerUnit,
                        Quantity = marketBoardItemListing.ItemQuantity,
                        RetainerCity = marketBoardItemListing.RetainerCityId
                    };

                    universalisListing.Materia = new List<UniversalisItemMateria>();
                    foreach (var itemMateria in marketBoardItemListing.Materia)
                        universalisListing.Materia.Add(new UniversalisItemMateria {
                            MateriaId = itemMateria.MateriaId,
                            SlotId = itemMateria.Index
                        });

                    listingsRequestObject.Listings.Add(universalisListing);
                }

                var upload = JsonConvert.SerializeObject(listingsRequestObject);
                client.UploadString(ApiBase + $"/upload/{ApiKey}", "POST", upload);
                //this.dalamud.Log(upload);

                var historyRequestObject = new UniversalisHistoryUploadRequest();
                historyRequestObject.WorldId = (int) this.dalamud.CurrentWorldId;
                historyRequestObject.UploaderId = uploader;
                historyRequestObject.ItemId = request.CatalogId;

                historyRequestObject.Entries = new List<UniversalisHistoryEntry>();
                foreach (var marketBoardHistoryListing in request.History)
                {
                    historyRequestObject.Entries.Add(new UniversalisHistoryEntry {
                        BuyerName = marketBoardHistoryListing.BuyerName,
                        Hq = marketBoardHistoryListing.IsHq,
                        OnMannequin = marketBoardHistoryListing.OnMannequin,
                        PricePerUnit = marketBoardHistoryListing.SalePrice,
                        Quantity = marketBoardHistoryListing.Quantity,
                        Timestamp = ((DateTimeOffset) marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds()
                    });
                }

                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                var historyUpload = JsonConvert.SerializeObject(historyRequestObject);
                client.UploadString(ApiBase + $"/upload/{ApiKey}", "POST", historyUpload);
                //this.dalamud.Log(historyUpload);

                this.dalamud.Log($"Universalis data upload for item#{request.CatalogId} to world#{historyRequestObject.WorldId} completed.");
            }
        }
    }
}
