// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MonitoringFunctions.Functions
{
    public static class DownloadPs1
    {
        private const string _monitorName = "download_ps1";
        private const string _url = "https://dot.net/v1/dotnet-install.ps1";

        [FunctionName("DownloadPs1")]
        public static async Task RunAsync([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            using IDataService dataService = new KustoDataService();
            await HelperMethods.CheckAndReportUrlAccess(log, _monitorName, _url, dataService).ConfigureAwait(false);
        }
    }
}
