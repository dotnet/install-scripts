// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.Models;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions
{
    internal interface IDataService : IDisposable
    {
        /// <summary>
        /// Stores the details of the <see cref="HttpResponseMessage"/> in the underlying data store.
        /// </summary>
        /// <param name="monitorName">Name of the monitor that will be associated with this data.</param>
        /// <param name="httpResponse">Http response data to be stored.</param>
        /// <returns>A task, tracking the initiated async operation. Errors should be reported through exceptions.</returns>
        Task ReportUrlAccessAsync(string monitorName, HttpResponseMessage httpResponse, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores the details of the <see cref="HttpRequestLogEntry"/> in the underlying data store.
        /// </summary>
        /// <param name="httpRequestLogEntry"><see cref="HttpRequestLogEntry"/> to be stored.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> instance.</param>
        /// <returns>A task, tracking the initiated async operation. Errors should be reported through exceptions.</returns>
        Task ReportUrlAccessAsync(HttpRequestLogEntry httpRequestLogEntry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores the details of the <see cref="HttpResponseMessage"/> in the underlying data store.
        /// </summary>
        /// <param name="monitorName">Name of the monitor that will be associated with this data.</param>
        /// <param name="httpResponse">Http response data to be stored.</param>
        /// <returns>A task, tracking the initiated async operation. Errors should be reported through exceptions.</returns>
        Task ReportScriptExecutionAsync(string monitorName, string scriptName, string commandLineArgs, string error, CancellationToken cancellationToken = default);
    }
}
