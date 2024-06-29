using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures
{
    public class MarketBoardHistory
    {
        public uint CatalogId;

        public class MarketBoardHistoryListing
        {
            public uint SalePrice;
            public DateTime PurchaseTime;
            public uint Quantity;
            public bool IsHq;
            public bool OnMannequin;
            public string BuyerName;
        }

        public List<MarketBoardHistoryListing> HistoryListings;

        public static MarketBoardHistory Read(byte[] message)
        {
            using var stream = new MemoryStream(message);
            using var reader = new BinaryReader(stream);

            var itemId = reader.ReadUInt32();
            var output = new MarketBoardHistory
            {
                CatalogId = itemId,
                HistoryListings = new List<MarketBoardHistoryListing>(),
            };

            for (var i = 0; i < 20; i++)
            {
                var salePrice = reader.ReadUInt32();
                if (salePrice == 0)
                {
                    // No additional sales for this item
                    break;
                }

                var listingEntry = new MarketBoardHistoryListing
                {
                    SalePrice = salePrice,
                    PurchaseTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()).UtcDateTime,
                    Quantity = reader.ReadUInt32(),
                    IsHq = reader.ReadBoolean(),
                    OnMannequin = reader.ReadBoolean(),
                    BuyerName = Encoding.UTF8.GetString(reader.ReadBytes(32)).TrimEnd('\u0000'),
                };

                // Read padding
                reader.ReadUInt16();

                output.HistoryListings.Add(listingEntry);
            }

            return output;
        }
    }
}