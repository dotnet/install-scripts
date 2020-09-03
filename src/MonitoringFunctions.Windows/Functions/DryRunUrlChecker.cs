// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MonitoringFunctions.Windows.Functions
{
    /// <summary>
    /// Runs the powershell script in -DryRun mode and checks whether the generated links are accessible
    /// </summary>
    internal static class DryRunUrlChecker
    {
        [FunctionName("DryRunLTS")]
        public static async Task RunLTSAsync([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string monitorName = "dry_run_LTS";
            string cmdArgs = "-c LTS";

            await HelperMethods.ExecuteDryRunCheckAndReportUrlAccessAsync(log, monitorName, cmdArgs).ConfigureAwait(false);
        }

        [FunctionName("DryRun3_1")]
        public static async Task Run3_1Async([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string monitorName = "dry_run_3_1";
            string cmdArgs = "-c 3.1";

            await HelperMethods.ExecuteDryRunCheckAndReportUrlAccessAsync(log, monitorName, cmdArgs).ConfigureAwait(false);
        }

        [FunctionName("DryRun3_0Runtime")]
        public static async Task Run3_0RuntimeAsync([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string monitorName = "dry_run_3_0_runtime";
            string cmdArgs = "-c 3.0 -Runtime dotnet";

            await HelperMethods.ExecuteDryRunCheckAndReportUrlAccessAsync(log, monitorName, cmdArgs).ConfigureAwait(false);
        }
    }
}
