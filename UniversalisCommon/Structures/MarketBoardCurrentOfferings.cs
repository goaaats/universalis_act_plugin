using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dalamud.Game.Network.Structures
{
    public class MarketBoardCurrentOfferings
    {
        public class MarketBoardItemListing
        {
            public ulong ListingId;
            public ulong RetainerId;
            public ulong RetainerOwnerId;
            public ulong ArtisanId;
            public uint PricePerUnit;
            public uint TotalTax;
            public uint ItemQuantity;
            public uint CatalogId;
            public DateTime LastReviewTime;

            public class ItemMateria
            {
                public int MateriaId;
                public int Index;
            }

            public List<ItemMateria> Materia;

            public string RetainerName;
            public string PlayerName;
            public bool IsHq;
            public int MateriaCount;
            public bool OnMannequin;
            public int RetainerCityId;
            public int StainId;
        }

        public List<MarketBoardItemListing> ItemListings;

        public int ListingIndexEnd;
        public int ListingIndexStart;
        public int RequestId;

        public static MarketBoardCurrentOfferings Read(byte[] message)
        {
            var output = new MarketBoardCurrentOfferings();

            using var stream = new MemoryStream(message);
            using var reader = new BinaryReader(stream);
            output.ItemListings = new List<MarketBoardItemListing>();

            for (var i = 0; i < 10; i++)
            {
                var listingEntry = new MarketBoardItemListing
                {
                    ListingId = reader.ReadUInt64(),
                    RetainerId = reader.ReadUInt64(),
                    RetainerOwnerId = reader.ReadUInt64(),
                    ArtisanId = reader.ReadUInt64(),
                    PricePerUnit = reader.ReadUInt32(),
                    TotalTax = reader.ReadUInt32(),
                    ItemQuantity = reader.ReadUInt32(),
                    CatalogId = reader.ReadUInt32(),
                    // Removed in 7.0
                    LastReviewTime = DateTime.UtcNow,
                };

                reader.ReadUInt16(); // retainer slot
                reader.ReadUInt16(); // durability
                reader.ReadUInt16(); // spiritbond

                listingEntry.Materia = new List<MarketBoardItemListing.ItemMateria>();
                for (var materiaIndex = 0; materiaIndex < 5; materiaIndex++)
                {
                    var materiaVal = reader.ReadUInt16();
                    var materiaEntry = new MarketBoardItemListing.ItemMateria
                    {
                        MateriaId = (materiaVal & 0xFF0) >> 4,
                        Index = materiaVal & 0xF,
                    };

                    if (materiaEntry.MateriaId != 0)
                    {
                        listingEntry.Materia.Add(materiaEntry);
                    }
                }

                // 6 bytes of padding
                reader.ReadUInt16();
                reader.ReadUInt32();

                listingEntry.RetainerName = Encoding.UTF8.GetString(reader.ReadBytes(32)).TrimEnd('\u0000');

                // Empty as of 7.0
                listingEntry.PlayerName = Encoding.UTF8.GetString(reader.ReadBytes(32)).TrimEnd('\u0000');

                listingEntry.IsHq = reader.ReadBoolean();
                listingEntry.MateriaCount = reader.ReadByte();
                listingEntry.OnMannequin = reader.ReadBoolean();
                listingEntry.RetainerCityId = reader.ReadByte();
                listingEntry.StainId = reader.ReadUInt16();

                // 4 bytes of padding
                reader.ReadUInt32();

                if (listingEntry.CatalogId != 0)
                {
                    output.ItemListings.Add(listingEntry);
                }
            }

            output.ListingIndexEnd = reader.ReadByte();
            output.ListingIndexStart = reader.ReadByte();
            output.RequestId = reader.ReadUInt16();

            return output;
        }
    }
}
