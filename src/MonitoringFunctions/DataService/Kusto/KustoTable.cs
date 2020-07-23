// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions.DataService.Kusto
{
    internal sealed class KustoTable<T> where T : IKustoTableRow
    {
        private KustoConnectionStringBuilder _connectionStringBuilder;

        private KustoQueuedIngestionProperties _ingestionProperties;

        public KustoTable(KustoConnectionStringBuilder connectionStringBuilder, string databaseName, string tableName, IJsonColumnMappingResolver columnMappingResolver)
            : this(connectionStringBuilder, databaseName, tableName, columnMappingResolver.GetColumnMappings<T>())
        {

        }

        public KustoTable(KustoConnectionStringBuilder connectionStringBuilder, string databaseName, string tableName, IEnumerable<ColumnMapping> columnMappings)
        {
            _connectionStringBuilder = connectionStringBuilder;

            _ingestionProperties = new KustoQueuedIngestionProperties(databaseName, tableName)
            {
                ReportLevel = IngestionReportLevel.FailuresOnly,
                ReportMethod = IngestionReportMethod.Queue,
                IngestionMapping = new IngestionMapping()
                {
                    IngestionMappingKind = global::Kusto.Data.Ingestion.IngestionMappingKind.Json,
                    IngestionMappings = columnMappings
                },
                Format = DataSourceFormat.json
            };
        }

        public async Task InsertRowAsync(T row, CancellationToken cancellationToken = default)
        {
            using IKustoQueuedIngestClient ingestClient = KustoIngestFactory.CreateQueuedIngestClient(_connectionStringBuilder);

            string serializedData = JsonConvert.SerializeObject(row);
            byte[] serializedBytes = Encoding.UTF8.GetBytes(serializedData);

            using MemoryStream dataStream = new MemoryStream(serializedBytes);

            // IKustoQueuedIngestClient doesn't support cancellation at the moment. Update the line below if it does in the future.
            await ingestClient.IngestFromStreamAsync(dataStream, _ingestionProperties, leaveOpen: true);
        }
    }
}
