using Newtonsoft.Json;
using System;
using System.Net;

namespace UniversalisCommon
{
    public class Definitions
    {
        public short PlayerSpawn { get; set; }
        public short PlayerSetup { get; set; }
        public short MarketBoardItemRequestStart { get; set; }
        public short MarketBoardOfferings { get; set; }
        public short MarketBoardHistory { get; set; }
        public short MarketTaxRates { get; set; }
        public short ContentIdNameMapResp { get; set; }

        private static readonly Uri DefinitionStoreUrl = new Uri("https://raw.githubusercontent.com/goaaats/universalis_act_plugin/master/definitions.json");

        public static Definitions Get()
        {
            using var client = new WebClient();
            try
            {
                var definitionJson = client.DownloadString(DefinitionStoreUrl);
                var deserializedDefinition = JsonConvert.DeserializeObject<Definitions>(definitionJson);

                return deserializedDefinition;
            }
            catch (WebException exc)
            {
                throw new Exception("Could not get definitions for Universalis.", exc);
            }
        }
    }
}
