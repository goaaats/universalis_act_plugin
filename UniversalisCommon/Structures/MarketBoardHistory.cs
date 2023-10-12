using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures
{
    public class MarketBoardHistory
    {
        public uint CatalogId;
        public uint CatalogId2;

        public class MarketBoardHistoryListing
        {
            public uint SalePrice;
            public DateTime PurchaseTime;
            public uint Quantity;
            public bool IsHq;
            public bool OnMannequin;

            public string BuyerName;

            public uint CatalogId;
        }

        public List<MarketBoardHistoryListing> HistoryListings;

        public static MarketBoardHistory Read(byte[] message)
        {
            using var stream = new MemoryStream(message);
            using var reader = new BinaryReader(stream);

            var output = new MarketBoardHistory
            {
                CatalogId = reader.ReadUInt32(),
                CatalogId2 = reader.ReadUInt32(),
                HistoryListings = new List<MarketBoardHistoryListing>(),
            };

            if (output.CatalogId2 == 0)
            {
                // No sales for this item yet
                return output;
            }

            for (var i = 0; i < 20; i++)
            {
                var listingEntry = new MarketBoardHistoryListing
                {
                    SalePrice = reader.ReadUInt32(),
                    PurchaseTime = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()).UtcDateTime,
                    Quantity = reader.ReadUInt32(),
                    IsHq = reader.ReadBoolean(),
                };

                reader.ReadBoolean();

                listingEntry.OnMannequin = reader.ReadBoolean();
                listingEntry.BuyerName = Encoding.UTF8.GetString(reader.ReadBytes(33)).TrimEnd('\u0000');
                listingEntry.CatalogId = reader.ReadUInt32();

                output.HistoryListings.Add(listingEntry);

                if (listingEntry.CatalogId == 0)
                    break;
            }

            return output;
        }
    }
}