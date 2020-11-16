// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.Models;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions.Providers
{
    internal sealed class DummyDataService : IDataService
    {
        public async Task ReportUrlAccessAsync(string monitorName, HttpResponseMessage httpResponse, CancellationToken cancellationToken = default)
        {
            await Task.Delay(new Random().Next(200, 4000), cancellationToken).ConfigureAwait(false);
        }

        public async Task ReportUrlAccessAsync(HttpRequestLogEntry httpRequestLogEntry, CancellationToken cancellationToken = default)
        {
            await Task.Delay(new Random().Next(200, 4000), cancellationToken).ConfigureAwait(false);
        }

        public async Task ReportScriptExecutionAsync(string monitorName, string scriptName, string commandLineArgs, string error, CancellationToken cancellationToken = default)
        {
            await Task.Delay(new Random().Next(200, 4000), cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Do nothing
        }

    }
}
