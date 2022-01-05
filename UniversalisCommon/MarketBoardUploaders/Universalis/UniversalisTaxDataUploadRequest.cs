using Newtonsoft.Json;

namespace Dalamud.Game.Network.MarketBoardUploaders.Universalis
{
    public class UniversalisTaxDataUploadRequest
    {
        /// <summary>
        /// Gets or sets the uploader's ID.
        /// </summary>
        [JsonProperty("uploaderID")]
        public string UploaderId { get; set; }

        /// <summary>
        /// Gets or sets the world to retrieve data from.
        /// </summary>
        [JsonProperty("worldID")]
        public uint WorldId { get; set; }

        /// <summary>
        /// Gets or sets tax data for each city's market.
        /// </summary>
        [JsonProperty("marketTaxRates")]
        public UniversalisTaxData TaxData { get; set; }
    }
}