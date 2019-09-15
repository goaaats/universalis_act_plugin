using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Dalamud.Game.Network;
using Dalamud.Game.Network.MarketBoardUploaders;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Newtonsoft.Json;
using UniversalisPlugin;

namespace UniversalisPlugin.MarketBoardUploaders.Universalis
{
    class FFXIVMBUploader : IMarketBoardUploader
    {
        private const string ApiBase = "https://ffxivmb.com";
        //private const string ApiBase = "http://localhost:5000";
        private const string ApiGuid = "c63cda16-7881-4644-8b67-bbef0c981e32";

        private UniversalisPlugin.UniversalisPluginControl dalamud;

        public FFXIVMBUploader(UniversalisPlugin.UniversalisPluginControl dalamud)
        {
            this.dalamud = dalamud;
        }

        public void Upload(MarketBoardItemRequest request)
        {
            using (var client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                this.dalamud.Log("Starting FFXIVMB upload.");
                var uploader = this.dalamud.LocalContentId;

                var listingsRequestObject = new UniversalisItemListingsUploadRequest();
                listingsRequestObject.WorldId = (int)this.dalamud.CurrentWorldId;
                listingsRequestObject.UploaderId = uploader;
                listingsRequestObject.ItemId = request.CatalogId;

                listingsRequestObject.Listings = new List<UniversalisItemListingsEntry>();
                foreach (var marketBoardItemListing in request.Listings)
                {
                    var universalisListing = new UniversalisItemListingsEntry
                    {
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
                        RetainerCity = marketBoardItemListing.RetainerCityId
                    };


                    universalisListing.Materia = new List<UniversalisItemMateria>();
                    foreach (var itemMateria in marketBoardItemListing.Materia)
                        universalisListing.Materia.Add(new UniversalisItemMateria
                        {
                            MateriaId = itemMateria.MateriaId,
                            SlotId = itemMateria.Index
                        });

                    listingsRequestObject.Listings.Add(universalisListing);
                }

                var upload = JsonConvert.SerializeObject(listingsRequestObject);
                client.UploadString(ApiBase + $"/uploadPrice?APIGuid={ApiGuid}", "POST", upload);
                //this.dalamud.Log(upload);

                var historyRequestObject = new UniversalisHistoryUploadRequest();
                historyRequestObject.WorldId = (int)this.dalamud.CurrentWorldId;
                historyRequestObject.UploaderId = uploader;
                historyRequestObject.ItemId = request.CatalogId;

                historyRequestObject.Entries = new List<UniversalisHistoryEntry>();
                foreach (var marketBoardHistoryListing in request.History)
                {
                    historyRequestObject.Entries.Add(new UniversalisHistoryEntry
                    {
                        BuyerName = marketBoardHistoryListing.BuyerName,
                        Hq = marketBoardHistoryListing.IsHq,
                        OnMannequin = marketBoardHistoryListing.OnMannequin,
                        PricePerUnit = marketBoardHistoryListing.SalePrice,
                        Quantity = marketBoardHistoryListing.Quantity,
                        Timestamp = ((DateTimeOffset)marketBoardHistoryListing.PurchaseTime).ToUnixTimeSeconds()
                    });
                }

                client.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                var historyUpload = JsonConvert.SerializeObject(historyRequestObject);
                client.UploadString(ApiBase + $"/uploadHistory?APIGuid={ApiGuid}", "POST", historyUpload);
                //this.dalamud.Log(historyUpload);

                this.dalamud.Log($"FFXIVMB data upload for item#{request.CatalogId} to world#{historyRequestObject.WorldId} completed.");
            }
        }

      
    }
}
