using Dalamud.Game.Network.Structures;
using System.Collections.Generic;

namespace Dalamud.Game.Network
{
    class MarketBoardItemRequest
    {
        public uint CatalogId { get; set; }
        public int AmountToArrive { get; set; }
        public bool HistoryReceived { get; set; }

        public List<MarketBoardCurrentOfferings.MarketBoardItemListing> Listings { get; set; }
        public List<MarketBoardHistory.MarketBoardHistoryListing> History { get; set; }

        public int ListingsRequestId { get; set; } = -1;

        public bool IsDone => Listings.Count == AmountToArrive && HistoryReceived;

        public bool IsNew => CatalogId == 0 && !IsDone;
    }
}
