using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis
{
    class UniversalisItemMateria
    {
        [JsonProperty("slotID")]
        public int SlotId { get; set; }
        
        [JsonProperty("materiaID")]
        public int MateriaId { get; set; }
    }
}
