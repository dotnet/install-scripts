// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
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
        Task ReportUrlAccess(string monitorName, HttpResponseMessage httpResponse);
    }
}
