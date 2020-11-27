// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions.Providers
{
    /// <summary>
    /// Mocks a data service. None of the method operate on a data store.
    /// Instead, they wait for a random duration and return.
    /// </summary>
    internal sealed class DummyDataService : IDataService
    {
        /// <inheritdoc/>
        public async Task ReportUrlAccessAsync(HttpRequestLogEntry httpRequestLogEntry, CancellationToken cancellationToken = default)
        {
            await Task.Delay(new Random().Next(200, 4000), cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
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
