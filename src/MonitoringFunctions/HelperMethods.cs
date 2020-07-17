// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MonitoringFunctions.Models;

[assembly:InternalsVisibleTo("MonitoringFunctions.Test, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]

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
        /// Tests if the contents of the given url can be accessed from current environment.
        /// Saves results to Kusto.
        /// </summary>
        /// <param name="log"><see cref="ILogger"/> that is used to report log information.</param>
        /// <param name="monitorName">Name of this monitor to be included in the logs and in the data sent to Kusto.</param>
        /// <param name="url">Url that this method will attempt to access.</param>
        /// <returns></returns>
        internal static async Task CheckAndReportUrlAccessAsync(ILogger log, string monitorName, string url, IDataService dataService, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

            await dataService.ReportUrlAccessAsync(monitorName, response, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                log.LogInformation($"Monitor '{monitorName}' has succeeded accessing the url {url}");
            }
            else
            {
                log.LogError($"Monitor '{monitorName}' has failed accessing the url {url} with error code {response.StatusCode}");
                throw new Exception($"Download failed with status code {response.StatusCode}. Monitor: {monitorName}");
            }
        }

        /// <summary>
        /// Executes dotnet-install.ps1 script with given arguments and returns the content of the output and error streams.
        /// </summary>
        internal static async Task<ScriptExecutionResult> ExecuteInstallScriptPs1Async(string? args = null)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo("powershell",
                @"-NoProfile -Command ""[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;" +
                @" $ProgressPreference = 'SilentlyContinue'; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing" +
                @" 'https://raw.githubusercontent.com/dotnet/install-scripts/master/src/dotnet-install.ps1')))" +
                $@" {args}""");

            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            Process installScriptProc = Process.Start(processStartInfo);
            string consoleOutput = await installScriptProc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            string consoleError = await installScriptProc.StandardError.ReadToEndAsync().ConfigureAwait(false);

            return new ScriptExecutionResult()
            {
                Output = consoleOutput,
                Error = consoleError
            };
        }
    }
}
