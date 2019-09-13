using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Network.Structures;

namespace Dalamud.Game.Network
{
    class MarketBoardItemRequest
    {
        public uint CatalogId { get; set; }
        public byte AmountToArrive { get; set; }

        public List<MarketBoardCurrentOfferings.MarketBoardItemListing> Listings { get; set; }
        public List<MarketBoardHistory.MarketBoardHistoryListing> History { get; set; }

        public int ListingsRequestId { get; set; } = -1;

        public bool IsDone => Listings.Count == AmountToArrive && History.Count != 0;
    }
}
