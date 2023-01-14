using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using Polly;

namespace UniversalisCommon
{
    public static class UpdateUtils
    {
        public static UpdateCheckResult UpdateCheck(Assembly applicationAssembly)
        {
            using var client = new WebClient();

            var remoteVersionStr = Policy
                .Handle<WebException>()
                .WaitAndRetry(3, retryAttempt => TimeSpan.FromSeconds(1))
                // ReSharper disable once AccessToDisposedClosure
                .ExecuteAndCapture(() => DownloadVersion(client));
            if (remoteVersionStr.Outcome == OutcomeType.Failure)
            {
                throw remoteVersionStr.FinalException;
            }

            if (!Version.TryParse(remoteVersionStr.Result, out var remoteVersion))
            {
                return UpdateCheckResult.RemoteVersionParsingFailed;
            }

            var assemblyVersion = GetAssemblyVersion(applicationAssembly);
            return assemblyVersion < remoteVersion
                ? UpdateCheckResult.NeedsUpdate
                : UpdateCheckResult.UpToDate;
        }

        private static string DownloadVersion(WebClient client)
        {
            return client.DownloadString(RemoteDataLocations.Version);
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