using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis
{
    class UniversalisHistoryUploadRequest
    {
        [JsonProperty("worldID")]
        public int WorldId { get; set; }

        [JsonProperty("itemID")]
        public uint ItemId { get; set; }

        [JsonProperty("entries")]
        public List<UniversalisHistoryEntry> Entries { get; set; }

        [JsonProperty("uploaderID")]
        public ulong UploaderId { get; set; }
    }
}
