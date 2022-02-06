using System.Net;

namespace UniversalisPlugin
{
    public static class UpdateUtils
    {
        public static bool CheckNeedsUpdate()
        {
            using var client = new WebClient();

            var remoteVersion =
                client.DownloadString(
                    "https://raw.githubusercontent.com/goaaats/universalis_act_plugin/master/version");

            return !remoteVersion.StartsWith(GetAssemblyVersion());
        }

        public static string GetAssemblyVersion()
        {
            return typeof(UniversalisPluginControl).Assembly.GetName().Version.ToString();
        }
    }
}