using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Network.Structures;

namespace Dalamud.Game.Network.MarketBoardUploaders
{
    interface IMarketBoardUploader {
        void Upload(MarketBoardItemRequest itemRequest);

        void UploadCrafterName(ulong contentId, string name);
    }
}
