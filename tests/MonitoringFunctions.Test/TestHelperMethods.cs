// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringFunctions.Providers;
using System.Threading.Tasks;

namespace MonitoringFunctions.Test
{
    [TestClass]
    public class TestHelperMethods
    {
        [TestMethod]
        public async Task TestCheckUrlAccess()
        {
            using IDataService dataService = new DummyDataService();
            // Test if we can access a highly available website without throwing an exception.
            await HelperMethods.CheckAndReportUrlAccessAsync(NullLogger.Instance, "test_run", "https://www.microsoft.com", dataService);
        }

        [DataRow("definitely not a url")]
        [DataRow("https://0.com")]
        [TestMethod]
        public async Task TestCheckUrlAccessFailure(string url)
        {
            try
            {
                using IDataService dataService = new DummyDataService();
                await HelperMethods.CheckAndReportUrlAccessAsync(NullLogger.Instance, "test_run", url, dataService);
                Assert.Fail();
            }
            catch
            {
                // Test passed
            }
        }
    }
}
