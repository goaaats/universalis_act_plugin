using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Network.MarketBoardUploaders.Universalis;
using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis
{
    class UniversalisItemListingsUploadRequest
    {
        [JsonProperty("worldID")]
        public int WorldId { get; set; }

        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        [JsonProperty("listings")]
        public List<UniversalisItemListingsEntry> Listings { get; set; }

        [JsonProperty("uploaderID")]
        public ulong UploaderId { get; set; }
    }
}
