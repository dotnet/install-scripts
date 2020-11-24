// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonitoringFunctions.Models;

namespace MonitoringFunctions
{
    internal static class HelperMethods
    {
        /// <summary>
        /// Single instance is sufficient for the whole app.
        /// From official docs: HttpClient is intended to be instantiated once and re-used throughout the life of an application.
        /// https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netcore-3.1#remarks
        /// </summary>
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Tests if the contents of the given url can be accessed from the current environment.
        /// Reports results to given data service.
        /// </summary>
        /// <param name="log"><see cref="ILogger"/> that is used to report log information.</param>
        /// <param name="monitorName">Name of this monitor to be included in the logs and in the data sent to Kusto.</param>
        /// <param name="url">Url that this method will attempt to access.</param>
        /// <param name="dataService">Data service to be used when reporting the results.</param>
        /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
        /// <returns>A task, tracking the initiated async operation. Errors should be reported through exceptions.</returns>
        internal static async Task CheckAndReportUrlAccessAsync(ILogger log,
            string monitorName,
            string url,
            IDataService dataService,
            CancellationToken cancellationToken = default)
        {
            _ = log ?? throw new ArgumentNullException(paramName: nameof(log));
            _ = string.IsNullOrWhiteSpace(monitorName) ? throw new ArgumentNullException(paramName: nameof(monitorName)) : monitorName;
            _ = string.IsNullOrWhiteSpace(url) ? throw new ArgumentNullException(paramName: nameof(url)) : url;
            _ = dataService ?? throw new ArgumentNullException(paramName: nameof(dataService));

            HttpRequestLogEntry logEntry = new HttpRequestLogEntry()
            {
                MonitorName = monitorName,
                EventTime = DateTime.UtcNow,
                RequestedUrl = url
            };
            HttpResponseMessage? response = null;
            try
            {
                response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                logEntry.HttpResponseCode = (int)response.StatusCode;
                await dataService.ReportUrlAccessAsync(logEntry, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception httpException)
            {
                if (response == null)
                {
                    // HttpClient failed to return a response, instead threw. The error should be in the exception.
                    logEntry.Error = httpException.Message;

                    try
                    {
                        await dataService.ReportUrlAccessAsync(logEntry, cancellationToken);
                    }
                    catch (Exception dataServiceException)
                    {
                        // Reporting to database has failed. Let's report into Azure Functions so that we can somehow track this.
                        log.LogError(httpException, $"Failed to access url {url} from monitor {monitorName}");
                        // There was an error with the data store and this should also be logged.
                        log.LogError(dataServiceException, "Failed to report an unsuccessful http request.");
                    }
                }
                else
                {
                    // Http request completed, but reporting to data service has failed.
                    log.LogError($"Failed to report an http response code {response.StatusCode} for url {url}.");
                }
            }
            finally
            {
                response?.Dispose();
            }
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
            string commandLineArgs = $"-DryRun {additionalCmdArgs}";

            using IDataService dataService = new DataServiceFactory().GetDataService();

            // Execute the script;
            ScriptExecutionResult results = await InstallScriptRunner.ExecuteInstallScriptAsync(commandLineArgs).ConfigureAwait(false);
            string scriptName = results.ScriptName;

            log.LogInformation($"Ouput stream: {results.Output}");

            if (!string.IsNullOrWhiteSpace(results.Error))
            {
                log.LogError($"Error stream: {results.Error}");
                await dataService.ReportScriptExecutionAsync(monitorName, scriptName, commandLineArgs, results.Error, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            // Parse the output
            ScriptDryRunResult dryRunResults = InstallScriptRunner.ParseDryRunOutput(results.Output);

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
    }
}
