// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringFunctions.Models;
using MonitoringFunctions.Providers;
using System;
using System.Threading.Tasks;

namespace MonitoringFunctions.Test
{
    [TestClass]
    public class TestHelperMethods
    {
        [TestMethod]
        public async Task TestCheckUrlAccessAsync()
        {
            using IDataService dataService = new DummyDataService();
            // Test if we can access a highly available website without throwing an exception.
            await HelperMethods.CheckAndReportUrlAccessAsync(NullLogger.Instance, "test_run", "https://www.microsoft.com", dataService);
        }

        [DataRow("definitely not a url")]
        [DataRow("https://0.com")]
        [TestMethod]
        public async Task TestCheckUrlAccessFailureAsync(string url)
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

        [DataRow("-DryRun -c 3.0")]
        [DataRow("-DryRun -c release/5.0.1xx-preview7")]
        [DataRow("-DryRun -Version LTS")]
        [TestMethod]
        public async Task TestExecuteInstallScriptAsync(string cmdArgs = "-DryRun")
        {
            try
            {
                ScriptExecutionResult executionResult = await InstallScriptRunner.ExecuteInstallScriptAsync(cmdArgs).ConfigureAwait(false);
                
                Assert.IsFalse(string.IsNullOrWhiteSpace(executionResult.Output), "Script execution hasn't returned an output.");
                Assert.IsTrue(string.IsNullOrWhiteSpace(executionResult.Error), $"Script execution has returned the following error: {executionResult.Error}");
            }
            catch (Exception e)
            {
                Assert.Fail($"Script execution has failed with an exception: {e.ToString()}");
            }
        }

        [DataRow("-DryRun -Version -Channel 3.0")]
        [DataRow("-Channel 12")]
        [DataRow("-switchThatDoesntExist")]
        [TestMethod]
        public async Task TestExecuteInstallScriptWrongArgsAsync(string cmdArgs = "-switchThatDoesntExist")
        {
            try
            {
                ScriptExecutionResult executionResult = await InstallScriptRunner.ExecuteInstallScriptAsync(cmdArgs).ConfigureAwait(false);

                Assert.IsFalse(string.IsNullOrWhiteSpace(executionResult.Error), $"Script execution hasn't returned any errors, but it should have." + 
                    $" Used command line arguments: " + cmdArgs);
            }
            catch (Exception e)
            {
                Assert.Fail($"Script execution has failed with an exception: {e.ToString()}");
            }
        }
    }
}
