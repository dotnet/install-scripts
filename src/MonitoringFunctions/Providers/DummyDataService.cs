using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonitoringFunctions.Providers
{
    internal class DummyDataService : IDataService
    {
        public async Task ReportUrlAccess(string monitorName, HttpResponseMessage httpResponse)
        {
            await Task.Delay(new Random().Next(200, 4000)).ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
