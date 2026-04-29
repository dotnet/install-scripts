// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using Install_Scripts.Test.Utils;
using Microsoft.NET.TestFramework.Assertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Tests for installing the .NET SDK via the install script.
    /// Runtime-type tests (dotnet, aspnetcore, windowsdesktop) live in
    /// <see cref="GivenThatIWantToInstallDotnetRuntimeFromAScript"/> so that xunit can run
    /// both collections in parallel, reducing overall wall-clock time.
    /// </summary>
    public class GivenThatIWantToInstallDotnetFromAScript : InstallScriptTestBase
    {
        public GivenThatIWantToInstallDotnetFromAScript(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper) { }

        [Theory]
        [MemberData(nameof(InstallSdkFromChannelTestCases), MemberType = typeof(InstallScriptTestBase))]
        public void WhenInstallingTheSdk(string channel, string? quality, string versionRegex)
        {
            // Run install script to download and install.
            var e = Encoding.UTF8;
            var args = GetInstallScriptArgs(channel, null, quality, _sdkInstallationDirectory);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = TestUtils.CreateDotnetCommand(dotnetArgs).ExecuteDotnetCommand(_sdkInstallationDirectory);

            // On MacOS, installation directory has an extra /private at the beginning.
            string installPathRegex = "\\[(/private)?" + Regex.Escape(Path.Combine(_sdkInstallationDirectory, "sdk")) + "\\]";
            string regex = Regex.Escape("  ") + versionRegex + Regex.Escape(" ") + installPathRegex;
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
            dotnetCommandResult.Should().Pass();
        }

        [Theory]
        [MemberData(nameof(InstallSdkFromChannelTestCases), MemberType = typeof(InstallScriptTestBase))]
        public void WhenInstallingTheSdkWithFeedCredential(string channel, string? quality, string versionRegex)
        {
            string feedCredential = "?" + Guid.NewGuid().ToString();

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, null, quality, _sdkInstallationDirectory, feedCredential, verboseLogging: true);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().NotHaveStdErr();
            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdOutContainingIgnoreCase(feedCredential);
            commandResult.Should().Pass();
        }

        [Theory]
        [InlineData("5.0.404-servicing.21560.14", "5.0.404")]
        [InlineData("6.0.100-preview.6.21364.34")]
        [InlineData("7.0.100-alpha.1.22054.9")]
        [InlineData("8.0.404")]
        [InlineData("9.0.100")]
        [InlineData("10.0.100")]
        [InlineData("11.0.100-preview.1.26104.118")]
        public void WhenInstallingASpecificVersionOfTheSdk(string version, string? effectiveVersion = null)
        {
            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel: null, runtime: null, quality: null, _sdkInstallationDirectory, version: version);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = TestUtils.CreateDotnetCommand(dotnetArgs).ExecuteDotnetCommand(_sdkInstallationDirectory);

            // On MacOS, installation directory has an extra /private at the beginning.
            string installPathRegex = "\\[(/private)?" + Regex.Escape(Path.Combine(_sdkInstallationDirectory, "sdk")) + "\\]";
            string regex = Regex.Escape("  " + (effectiveVersion ?? version) + " ") + installPathRegex;
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
            dotnetCommandResult.Should().Pass();
        }

        [Theory]
        [InlineData("5.0.404-servicing.21560.14", "5.0.404")]
        [InlineData("6.0.100-preview.6.21364.34")]
        [InlineData("7.0.100-alpha.1.22054.9")]
        [InlineData("8.0.11", null, "aspnetcore")]
        [InlineData("9.0.0", null, "aspnetcore")]
        [InlineData("10.0.0", null, "aspnetcore")]
        [InlineData("5.0.13-servicing.21552.32", "5.0.13", "aspnetcore")]
        [InlineData("6.0.0-preview.4.21176.7", null, "aspnetcore")]
        [InlineData("7.0.0-alpha.1.21567.15", null, "aspnetcore")]
        [InlineData("5.0.13-servicing.21560.6", "5.0.13", "dotnet")]
        [InlineData("6.0.0-preview.4.21176.7", null, "dotnet")]
        [InlineData("7.0.0-alpha.1.21528.8", null, "dotnet")]
        [InlineData("6.0.1-servicing.21568.2", "6.0.1", "windowsdesktop")]
        [InlineData("7.0.0-alpha.1.21472.1", null, "windowsdesktop")]
        public void WhenInstallingAnAlreadyInstalledVersion(string version, string? effectiveVersion = null, string? runtime = null)
        {
            if (runtime == "windowsdesktop" && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Don't install windowsdesktop if not on Windows.
                return;
            }

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel: null, runtime, quality: null, _sdkInstallationDirectory, version: version);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();

            // Run the same command. This time, it should say "already installed".
            commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().NotHaveStdOutContaining("Installation finished");
            commandResult.Should().HaveStdOutContaining($"with version '{effectiveVersion ?? version}' is already installed.");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();
        }

        [Theory]
        [InlineData(null, "2.4", "ga")]
        [InlineData(null, "3.9", null)]
        [InlineData(null, "6.a", "preview")]
        [InlineData(null, "release/1.4.1xx", null)]
        [InlineData(null, "LTS", "invalidQuality")]
        [InlineData("dotnet", "2.4", "ga")]
        [InlineData("dotnet", "3.9", null)]
        [InlineData("dotnet", "6.a", "preview")]
        [InlineData("dotnet", "release/1.4.1xx", null)]
        [InlineData("dotnet", "LTS", "invalidQuality")]
        public void WhenFailingToInstallWithFeedCredentials(string? runtime, string channel, string? quality)
        {
            string feedCredential = "?" + Guid.NewGuid().ToString();

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, runtime, quality, _sdkInstallationDirectory, feedCredential, verboseLogging: true);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdErr();
            commandResult.Should().NotHaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdOutContainingIgnoreCase(feedCredential);
        }

        [Theory]
        [InlineData("8.0.303", Quality.Daily)]
        public void WhenBothVersionAndQualityWereSpecified(string version, Quality quality)
        {
            var args = GetInstallScriptArgs(null, null, quality.ToString(), _sdkInstallationDirectory, version: version);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Fail();
            commandResult.Should().HaveStdErrContaining("Quality and Version options are not allowed to be specified simultaneously.");
            commandResult.Should().NotHaveStdOutContaining("Installation finished");
        }

        [Theory]
        [InlineData("8.0.303", null)]
        public void WhenEitherVersionOrQualityWasSpecified(string? version, Quality? quality)
        {
            // Run install script to download and install.
            var args = GetInstallScriptArgs(null, null, quality?.ToString(), _sdkInstallationDirectory, version: version);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();
        }

        [Fact]
        public void WhenNoArgsWereSpecified()
        {
            var args = GetInstallScriptArgs(null, null, null, installDir: _sdkInstallationDirectory);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();
        }
    }
}
