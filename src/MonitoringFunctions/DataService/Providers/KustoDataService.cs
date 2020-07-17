// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data;
using MonitoringFunctions.DataService.Kusto;
using MonitoringFunctions.Models;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions
{
    internal sealed class KustoDataService : IDataService
    {
        private const string ServiceNameAndRegion = "dotnetinstallcluster.eastus2";
        private const string DatabaseName = "dotnet_install_monitoring_database";

        private readonly KustoTable<HttpRequestLogEntry> HttpRequestLogsTable;
        private readonly KustoTable<ScriptExecutionLogEntry> ScriptExecutionLogsTable;

        internal KustoDataService()
        {
            KustoConnectionStringBuilder kcsb = new KustoConnectionStringBuilder($"https://ingest-{ServiceNameAndRegion}.kusto.windows.net")
                .WithAadManagedIdentity("system");

            DirectJsonMappingResolver directJsonMappingResolver = new DirectJsonMappingResolver();

            HttpRequestLogsTable = new KustoTable<HttpRequestLogEntry>(kcsb, DatabaseName, "UrlAccessLogs", directJsonMappingResolver);
            ScriptExecutionLogsTable = new KustoTable<ScriptExecutionLogEntry>(kcsb, DatabaseName, "ScriptExecLogs", directJsonMappingResolver);
        }

        /// <summary>
        /// Reports the details of the <see cref="HttpResponseMessage"/> to kusto.
        /// </summary>
        /// <param name="monitorName">Name of the monitor generating this data entry.</param>
        /// <param name="httpResponse">Response to be reported.</param>
        /// <returns>A task, tracking this async operation.</returns>
        public async Task ReportUrlAccessAsync(string monitorName, HttpResponseMessage httpResponse, CancellationToken cancellationToken = default)
        {
            HttpRequestLogEntry logEntry = new HttpRequestLogEntry()
            {
                MonitorName = monitorName,
                EventTime = DateTime.UtcNow,
                RequestedUrl = httpResponse.RequestMessage.RequestUri.AbsoluteUri,
                HttpResponseCode = (int)httpResponse.StatusCode
            };

            await HttpRequestLogsTable.InsertRow(logEntry, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports the details of a script execution to kusto.
        /// </summary>
        /// <param name="monitorName">Name of the monitor generating this data entry.</param>
        /// <param name="scriptName">Name of the script that was executed.</param>
        /// <param name="commandLineArgs">Command line arguments passed to the script at the moment of execution.</param>
        /// <param name="error">Errors that occured during the execution, if any.</param>
        /// <returns>A task, tracking this async operation.</returns>
        public async Task ReportScriptExecutionAsync(string monitorName, string scriptName, string commandLineArgs, string error, CancellationToken cancellationToken = default)
        {
            ScriptExecutionLogEntry logEntry = new ScriptExecutionLogEntry()
            {
                MonitorName = monitorName,
                EventTime = DateTime.UtcNow,
                ScriptName = scriptName,
                CommandLineArgs = commandLineArgs,
                Error = error
            };

            await ScriptExecutionLogsTable.InsertRow(logEntry, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
