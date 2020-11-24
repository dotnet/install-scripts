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
        /// Stores the details of the <see cref="HttpRequestLogEntry"/> in the underlying data store.
        /// </summary>
        /// <param name="httpRequestLogEntry"><see cref="HttpRequestLogEntry"/> to be stored.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> instance.</param>
        /// <returns>A task, tracking the initiated async operation. Errors should be reported through exceptions.</returns>
        Task ReportUrlAccessAsync(HttpRequestLogEntry httpRequestLogEntry, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores the details of an install-script execution in the underlying data store.
        /// </summary>
        /// <param name="monitorName">Name of the monitor generating this data entry.</param>
        /// <param name="scriptName">Name of the script that was executed.</param>
        /// <param name="commandLineArgs">Command line arguments passed to the script at the moment of execution.</param>
        /// <param name="error">Errors that occured during the execution, if any.</param>
        /// <returns>A task, tracking the initiated async operation. Errors should be reported through exceptions.</returns>
        Task ReportScriptExecutionAsync(string monitorName, string scriptName, string commandLineArgs, string error, CancellationToken cancellationToken = default);
    }
}
