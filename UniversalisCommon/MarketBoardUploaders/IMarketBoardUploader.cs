using Dalamud.Game.Network.MarketBoardUploaders.Universalis;

namespace Dalamud.Game.Network.MarketBoardUploaders
{
    interface IMarketBoardUploader
    {
        void Upload(MarketBoardItemRequest itemRequest);

        void UploadTaxRates(UniversalisTaxDataUploadRequest taxRatesRequest);

        void UploadCrafterName(ulong contentId, string name);
    }
}
