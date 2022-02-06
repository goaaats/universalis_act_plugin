using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;

namespace UniversalisCommon
{
    public static class UpdateUtils
    {
        public static UpdateCheckResult UpdateCheck(Assembly applicationAssembly)
        {
            using var client = new WebClient();

            var remoteVersionStr =
                client.DownloadString(
                    "https://raw.githubusercontent.com/goaaats/universalis_act_plugin/master/version");

            if (!Version.TryParse(remoteVersionStr, out var remoteVersion))
            {
                return UpdateCheckResult.RemoteVersionParsingFailed;
            }

            return remoteVersion < GetAssemblyVersion(applicationAssembly)
                ? UpdateCheckResult.NeedsUpdate
                : UpdateCheckResult.UpToDate;
        }

        private static Version GetAssemblyVersion(Assembly applicationAssembly)
        {
            return applicationAssembly.GetName().Version;
        }

        public static void OpenLatestReleaseInBrowser()
        {
            Process.Start("https://github.com/goaaats/universalis_act_plugin/releases/latest");
        }
    }
}