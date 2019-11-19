using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace UniversalisPlugin
{
    public class Definitions
    {
        public short PlayerSpawn = 0x243;
        public short PlayerSetup = 0x1A1;
        public short MarketBoardItemRequestStart = 0x0F2;
        public short MarketBoardOfferings = 0x1E2;
        public short MarketBoardHistory = 0x123;
        public short ContentIdNameMapResp = 0x199;

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
