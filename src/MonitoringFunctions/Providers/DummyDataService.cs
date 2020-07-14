﻿// Copyright (c) Microsoft. All rights reserved.

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

        public void Dispose()
        {
            // Do nothing
        }
    }
}
