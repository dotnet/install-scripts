// Copyright (c) Microsoft. All rights reserved.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using MonitoringFunctions.Models;
using Kusto.Cloud.Platform.IO;
using System.Threading;

namespace MonitoringFunctions.Functions
{
    /// <summary>
    /// Runs the scripts in -DryRun mode and checks weather the generated links are accessible
    /// </summary>
    internal static class DryRunUrlChecker
    {
        [FunctionName("DryRunLTS")]
        public static async Task RunLTSAsync([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string monitorName = "dry_run_LTS";
            string cmdArgs = "-c LTS";

            await ExecuteDryRunCheckAndReportUrlAccessAsync(log, monitorName, cmdArgs).ConfigureAwait(false);
        }

        [FunctionName("DryRun3_1")]
        public static async Task Run3_1Async([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string monitorName = "dry_run_3_1";
            string cmdArgs = "-c 3.1";

            await ExecuteDryRunCheckAndReportUrlAccessAsync(log, monitorName, cmdArgs).ConfigureAwait(false);
        }
        
        [FunctionName("DryRun3_0Runtime")]
        public static async Task Run3_0RuntimeAsync([TimerTrigger("0 */30 * * * *")] TimerInfo myTimer, ILogger log)
        {
            string monitorName = "dry_run_3_0_runtime";
            string cmdArgs = "-c 3.0 -Runtime dotnet";

            await ExecuteDryRunCheckAndReportUrlAccessAsync(log, monitorName, cmdArgs).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the Ps1 script with DryDun switch,
        /// Parses the output to acquire primary and legacy Urls,
        /// Tests the primary Url to see if it is available,
        /// Reports the results to the data service provided.
        /// </summary>
        internal static async Task ExecuteDryRunCheckAndReportUrlAccessAsync(ILogger log, string monitorName, string additionalCmdArgs,
            CancellationToken cancellationToken = default)
        {
            string scriptName = "dotnet-install.ps1";
            string commandLineArgs = $"-DryRun {additionalCmdArgs}";

            using IDataService dataService = new DataServiceFactory().GetDataService();
            
            // Execute the script;
            ScriptExecutionResult results = await HelperMethods.ExecuteInstallScriptPs1Async(commandLineArgs).ConfigureAwait(false);

            log.LogInformation($"Ouput stream: {results.Output}");

            if (!string.IsNullOrWhiteSpace(results.Error))
            {
                log.LogError($"Error stream: {results.Error}");
                await dataService.ReportScriptExecutionAsync(monitorName, scriptName, commandLineArgs, results.Error, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            // Parse the output
            ScriptDryRunResult dryRunResults = ParseDryRunOutput(results.Output);

            if (string.IsNullOrWhiteSpace(dryRunResults.PrimaryUrl))
            {
                log.LogError($"Primary Url was not found for channel {additionalCmdArgs}");
                await dataService.ReportScriptExecutionAsync(monitorName, scriptName, commandLineArgs,
                    "Failed to parse primary url from the following DryRun execution output: " + results.Output
                    , cancellationToken).ConfigureAwait(false);
                return;
            }

            // Validate URL accessibility
            await HelperMethods.CheckAndReportUrlAccessAsync(log, monitorName, dryRunResults.PrimaryUrl, dataService);
        }

        /// <summary>
        /// Parses the output of the script when executed in DryRun mode and finds out Primary and Legacy urls.
        /// </summary>
        /// <param name="output">Output of the script execution in DryRun mode</param>
        /// <returns>Object containing primary and legacy runs</returns>
        internal static ScriptDryRunResult ParseDryRunOutput(string? output)
        {
            string primaryUrlIdentifier = "Primary named payload URL: ";
            string legacyUrlIdentifier = "Legacy named payload URL: ";

            ScriptDryRunResult result = new ScriptDryRunResult();

            using StringStream stringStream = new StringStream(output);
            using StreamReader streamReader = new StreamReader(stringStream);

            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                // Does this line contain the primary url?
                int primaryIdIndex = line.IndexOf(primaryUrlIdentifier);
                if (primaryIdIndex != -1)
                {
                    result.PrimaryUrl = line.Substring(primaryIdIndex + primaryUrlIdentifier.Length);
                }
                else
                {
                    // Does this line contain the legacy url?
                    int legacyIdIndex = line.IndexOf(legacyUrlIdentifier);
                    if(legacyIdIndex != -1)
                    {
                        result.LegacyUrl = line.Substring(legacyIdIndex + legacyUrlIdentifier.Length);
                    }
                }
            }

            return result;
        }
    }
}
