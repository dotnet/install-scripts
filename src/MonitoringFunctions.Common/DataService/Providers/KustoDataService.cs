// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data;
using MonitoringFunctions.DataService.Kusto;
using MonitoringFunctions.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions
{
    internal sealed class KustoDataService : IDataService
    {
        private const string ServiceNameAndRegion = "dotnetinstallcluster.eastus2";
        private static readonly string? DatabaseName = Environment.GetEnvironmentVariable("kusto_db_name");

        private readonly KustoTable<HttpRequestLogEntry> _httpRequestLogsTable;
        private readonly KustoTable<ScriptExecutionLogEntry> _scriptExecutionLogsTable;

        internal KustoDataService()
        {
            if (string.IsNullOrWhiteSpace(DatabaseName))
            {
                throw new InvalidOperationException($"{nameof(DatabaseName)} was not correctly configured. " +
                    "Make sure \"kusto_db_name\" is properly assigned in function app configuration.");
            }

            KustoConnectionStringBuilder kcsb = new KustoConnectionStringBuilder($"https://ingest-{ServiceNameAndRegion}.kusto.windows.net")
                .WithAadManagedIdentity("system");

            DirectJsonMappingResolver directJsonMappingResolver = new DirectJsonMappingResolver();

            _httpRequestLogsTable = new KustoTable<HttpRequestLogEntry>(kcsb, DatabaseName, "UrlAccessLogs", directJsonMappingResolver);
            _scriptExecutionLogsTable = new KustoTable<ScriptExecutionLogEntry>(kcsb, DatabaseName, "ScriptExecLogs", directJsonMappingResolver);
        }

        /// <summary>
        /// Stores the details of the <see cref="HttpRequestLogEntry"/> in the underlying kusto database.
        /// </summary>
        /// <inheritdoc/>
        public async Task ReportUrlAccessAsync(HttpRequestLogEntry httpRequestLogEntry, CancellationToken cancellationToken = default)
        {
            await _httpRequestLogsTable.InsertRowAsync(httpRequestLogEntry, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Stores the details of an install-script execution in the underlying kusto database.
        /// </summary>
        /// <inheritdoc/>
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

            await _scriptExecutionLogsTable.InsertRowAsync(logEntry, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
