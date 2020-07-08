// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
using MonitoringFunctions.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions
{
    internal sealed class KustoDataService : IDataService
    {
        private const string ServiceNameAndRegion = "dotnetinstallcluster.eastus2";
        private const string DatabaseName = "dotnet_install_monitoring_database";
        private const string TableName = "DaemonLogs";

        private static IEnumerable<ColumnMapping> HttpRequestLogColumnMapping { get; }

        static KustoDataService()
        {
            HttpRequestLogColumnMapping = new ColumnMapping[]
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
        public async Task ReportUrlAccessAsync(string monitorName, HttpResponseMessage httpResponse, CancellationToken cancellationToken = default)
        {
            HttpRequestLogEntry logEntry = new HttpRequestLogEntry()
            {
                MonitorName = monitorName,
                EventTime = DateTime.UtcNow,
                RequestedUrl = httpResponse.RequestMessage.RequestUri.AbsoluteUri,
                HttpStatusCode = (int)httpResponse.StatusCode
            };

            KustoConnectionStringBuilder kcsb = new KustoConnectionStringBuilder($"https://ingest-{ServiceNameAndRegion}.kusto.windows.net")
                .WithAadManagedIdentity("system");

            using IKustoQueuedIngestClient ingestClient = KustoIngestFactory.CreateQueuedIngestClient(kcsb);
            KustoQueuedIngestionProperties ingestProps = new KustoQueuedIngestionProperties(DatabaseName, TableName);

            ingestProps.ReportLevel = IngestionReportLevel.FailuresOnly;
            ingestProps.ReportMethod = IngestionReportMethod.Queue;
            ingestProps.IngestionMapping = new IngestionMapping()
            {
                IngestionMappingKind = Kusto.Data.Ingestion.IngestionMappingKind.Json,
                IngestionMappings = HttpRequestLogColumnMapping
            };
            ingestProps.Format = DataSourceFormat.json;

            using MemoryStream memStream = new MemoryStream();
            using StreamWriter writer = new StreamWriter(memStream);

            writer.WriteLine(JsonConvert.SerializeObject(logEntry));

            writer.Flush();
            memStream.Seek(0, SeekOrigin.Begin);

            // IKustoQueuedIngestClient doesn't support cancellation at the moment. Update the line below if it does in the future.
            await ingestClient.IngestFromStreamAsync(memStream, ingestProps, leaveOpen: true);
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
