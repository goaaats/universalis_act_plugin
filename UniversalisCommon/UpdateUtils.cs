using System.Net;
using System.Reflection;

namespace UniversalisCommon
{
    public static class UpdateUtils
    {
        public static bool CheckNeedsUpdate(Assembly applicationAssembly)
        {
            using var client = new WebClient();

            var remoteVersion =
                client.DownloadString(
                    "https://raw.githubusercontent.com/goaaats/universalis_act_plugin/master/version");

            return !remoteVersion.StartsWith(GetAssemblyVersion(applicationAssembly));
        }

        public static string GetAssemblyVersion(Assembly applicationAssembly)
        {
            return applicationAssembly.GetName().Version.ToString();
        }
    }
}