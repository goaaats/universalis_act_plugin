using System;
using System.Net;
using Newtonsoft.Json;

namespace UniversalisPlugin
{
    public class Definitions
    {
        public short PlayerSpawn = 0x0DC;
        public short PlayerSetup = 0x3B4;
        public short MarketBoardItemRequestStart = 0x349;
        public short MarketBoardOfferings = 0x130;
        public short MarketBoardHistory = 0x1F7;
        public short ContentIdNameMapResp = 0x172;

        private static readonly Uri DefinitionStoreUrl = new Uri("https://raw.githubusercontent.com/goaaats/universalis_act_plugin/master/definitions.json");

        public static string GetJson() => JsonConvert.SerializeObject(new Definitions());

        public static Definitions Get()
        {
            using (WebClient client = new WebClient())
            {
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
}
