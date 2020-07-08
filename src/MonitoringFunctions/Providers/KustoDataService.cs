﻿// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonitoringFunctions
{
    internal class KustoDataService : IDataService
    {
        private const string serviceNameAndRegion = "dotnetinstallcluster.eastus2";
        private const string databaseName = "dotnet_install_monitoring_database";
        private const string tableName = "DaemonLogs";

        private static IEnumerable<ColumnMapping> DaemonLogsJsonMapping { get; }

        static KustoDataService()
        {
            DaemonLogsJsonMapping = new List<ColumnMapping>()
                {
                    new ColumnMapping()
                    {
                        ColumnName = "monitor_name",
                        Properties = new Dictionary<string, string>() { { "Path", "$.monitor_name" } }
                    },
                    new ColumnMapping()
                    {
                        ColumnName = "timestamp",
                        Properties = new Dictionary<string, string>() { { "Path", "$.timestamp" } }
                    },
                    new ColumnMapping()
                    {
                        ColumnName = "requested_url",
                        Properties = new Dictionary<string, string>() { { "Path", "$.requested_url" } }
                    },
                    new ColumnMapping()
                    {
                        ColumnName = "http_response_code",
                        Properties = new Dictionary<string, string>() { { "Path", "$.http_response_code" } }
                    },
                    new ColumnMapping()
                    {
                        ColumnName = "cmd_args",
                        Properties = new Dictionary<string, string>() { { "Path", "$.cmd_args" } }
                    },
                    new ColumnMapping()
                    {
                        ColumnName = "error",
                        Properties = new Dictionary<string, string>() { { "Path", "$.error" } }
                    }
                };
        }

        internal KustoDataService() { }

        /// <summary>
        /// Saves the details of the <see cref="HttpResponseMessage"/> to 
        /// </summary>
        /// <param name="monitorName"></param>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public async Task ReportUrlAccess(string monitorName, HttpResponseMessage httpResponse)
        {
            KustoConnectionStringBuilder kcsb = new KustoConnectionStringBuilder($"https://ingest-{serviceNameAndRegion}.kusto.windows.net")
                .WithAadManagedIdentity("system");

            using IKustoQueuedIngestClient ingestClient = KustoIngestFactory.CreateQueuedIngestClient(kcsb);
            KustoQueuedIngestionProperties ingestProps = new KustoQueuedIngestionProperties(databaseName, tableName);

            ingestProps.ReportLevel = IngestionReportLevel.FailuresOnly;
            ingestProps.ReportMethod = IngestionReportMethod.Queue;
            ingestProps.IngestionMapping = new IngestionMapping()
            {
                IngestionMappingKind = Kusto.Data.Ingestion.IngestionMappingKind.Json,
                IngestionMappings = DaemonLogsJsonMapping
            };
            ingestProps.Format = DataSourceFormat.json;

            using MemoryStream memStream = new MemoryStream();
            using StreamWriter writer = new StreamWriter(memStream);

            writer.WriteLine(
                @"{{ ""monitor_name"":""{0}"", ""timestamp"":""{1}"", " +
                @"""requested_url"":""{2}"", ""http_response_code"":""{3}"" }}",
                monitorName, DateTime.UtcNow,
                httpResponse.RequestMessage.RequestUri, (int)httpResponse.StatusCode);

            writer.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            await ingestClient.IngestFromStreamAsync(memStream, ingestProps, leaveOpen: true).ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
