// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using Install_Scripts.Test.Utils;
using Microsoft.NET.TestFramework.Assertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Tests for installing .NET runtime types (dotnet, aspnetcore, windowsdesktop) via the install script.
    /// Kept in a separate class from SDK tests to enable xunit parallel collection execution,
    /// reducing overall test run time by running SDK and runtime tests concurrently.
    /// </summary>
    public class GivenThatIWantToInstallDotnetRuntimeFromAScript : InstallScriptTestBase
    {
        public GivenThatIWantToInstallDotnetRuntimeFromAScript(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper) { }

        [Theory]
        [MemberData(nameof(InstallRuntimeFromChannelTestCases), MemberType = typeof(InstallScriptTestBase))]
        public void WhenInstallingDotnetRuntime(string channel, string? quality, string versionRegex)
        {
            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, "dotnet", quality, _sdkInstallationDirectory);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = TestUtils.CreateDotnetCommand(dotnetArgs).ExecuteDotnetCommand(_sdkInstallationDirectory);

            string lineStartRegex = Regex.Escape(" Microsoft.NETCore.App ");
            string lineEndRegex = "\\ \\[(/private)?" + Regex.Escape(Path.Combine(_sdkInstallationDirectory, "shared", "Microsoft.NETCore.App")) + "\\]";
            string regex = lineStartRegex + versionRegex + lineEndRegex;
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
            dotnetCommandResult.Should().Pass();
        }

        [Theory]
        [MemberData(nameof(InstallRuntimeFromChannelTestCases), MemberType = typeof(InstallScriptTestBase))]
        public void WhenInstallingAspNetCoreRuntime(string channel, string? quality, string versionRegex)
        {
            if (channel == "release/3.0"
                || channel == "release/3.1")
            {
                // These scenarios are broken.
                return;
            }

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, "aspnetcore", quality, _sdkInstallationDirectory);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = TestUtils.CreateDotnetCommand(dotnetArgs).ExecuteDotnetCommand(_sdkInstallationDirectory);

            string lineStartRegex = Regex.Escape(" Microsoft.AspNetCore.App ");
            string lineEndRegex = "\\ \\[(/private)?" + Regex.Escape(Path.Combine(_sdkInstallationDirectory, "shared", "Microsoft.AspNetCore.App")) + "\\]";
            string regex = lineStartRegex + versionRegex + lineEndRegex;
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
            dotnetCommandResult.Should().Pass();
        }

        [Theory]
        [MemberData(nameof(InstallRuntimeFromChannelTestCases), MemberType = typeof(InstallScriptTestBase))]
        public void WhenInstallingWindowsdesktopRuntime(string channel, string? quality, string versionRegex)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Don't install windowsdesktop if not on Windows.
                return;
            }

            List<Regex> exclusions = new List<Regex>()
            {
                new Regex(".*2\\..*"),     // Runtime is not supported in this version.
                new Regex(".*3\\.0.*"),    // Runtime is not supported in this version.
                new Regex("release/3.1"),  // Broken scenario.
                new Regex("6.0"),          // Broken scenario.
                new Regex("6.0-preview2"), // Broken scenario.
            };

            if (exclusions.Any(e => e.IsMatch(channel)))
            {
                // Test is excluded.
                return;
            }

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, "windowsdesktop", quality, _sdkInstallationDirectory);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            //commandResult.Should().NotHaveStdErr();
            //commandResult.Should().HaveStdOutContaining("Installation finished");
            //commandResult.Should().Pass();

            //TestOutputHelper.PopulateTestLoggerOutput(outputHelper, commandResult);

            // Dotnet CLI is not included in the windowsdesktop runtime. Therefore, version validation cannot be tested.
            // Add the validation once it becomes available in the artifacts.
        }

        [Theory]
        [MemberData(nameof(InstallRuntimeFromChannelTestCases), MemberType = typeof(InstallScriptTestBase))]
        public void WhenInstallingDotnetRuntimeWithFeedCredential(string channel, string? quality, string versionRegex)
        {
            string feedCredential = "?" + Guid.NewGuid().ToString();

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, "dotnet", quality, _sdkInstallationDirectory, feedCredential, verboseLogging: true);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().NotHaveStdErr();
            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdOutContainingIgnoreCase(feedCredential);
            commandResult.Should().Pass();
        }

        [Theory]
        [InlineData("5.0.13-servicing.21560.6", "5.0.13")]
        [InlineData("6.0.0-preview.4.21176.7")]
        [InlineData("7.0.0-alpha.1.21528.8")]
        [InlineData("8.0.11")]
        [InlineData("9.0.0")]
        [InlineData("10.0.0")]
        [InlineData("11.0.0-preview.1.26104.118")]
        public void WhenInstallingASpecificVersionOfDotnetRuntime(string version, string? effectiveVersion = null)
        {
            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel: null, "dotnet", quality: null, _sdkInstallationDirectory, version: version);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = TestUtils.CreateDotnetCommand(dotnetArgs).ExecuteDotnetCommand(_sdkInstallationDirectory);

            string lineStartRegex = Regex.Escape(" Microsoft.NETCore.App ");
            string lineEndRegex = "\\ \\[(/private)?" + Regex.Escape(Path.Combine(_sdkInstallationDirectory, "shared", "Microsoft.NETCore.App")) + "\\]";
            string regex = lineStartRegex + Regex.Escape(effectiveVersion ?? version) + lineEndRegex;
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
            dotnetCommandResult.Should().Pass();
        }

        [Theory]
        [InlineData("5.0.13-servicing.21552.32", "5.0.13")]
        [InlineData("6.0.0-preview.4.21176.7")]
        [InlineData("7.0.0-alpha.1.21567.15")]
        [InlineData("8.0.11")]
        [InlineData("9.0.0")]
        [InlineData("10.0.0")]
        [InlineData("11.0.0-preview.1.26104.118")]
        public void WhenInstallingASpecificVersionOfAspNetCoreRuntime(string version, string? effectiveVersion = null)
        {
            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel: null, "aspnetcore", quality: null, _sdkInstallationDirectory, version: version);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().NotHaveStdErr();
            commandResult.Should().Pass();

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = TestUtils.CreateDotnetCommand(dotnetArgs).ExecuteDotnetCommand(_sdkInstallationDirectory);

            string lineStartRegex = Regex.Escape(" Microsoft.AspNetCore.App ");
            string lineEndRegex = "\\ \\[(/private)?" + Regex.Escape(Path.Combine(_sdkInstallationDirectory, "shared", "Microsoft.AspNetCore.App")) + "\\]";
            string regex = lineStartRegex + Regex.Escape(effectiveVersion ?? version) + lineEndRegex;
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
            dotnetCommandResult.Should().Pass();
        }

        [Theory]
        // productVersion files are broken prior to 6.0 release.
        // [InlineData("5.0.14-servicing.21614.9")]
        [InlineData("6.0.1-servicing.21568.2")]
        [InlineData("7.0.0-alpha.1.21472.1")]
        [InlineData("8.0.11")]
        [InlineData("9.0.0")]
        [InlineData("10.0.0")]
        [InlineData("11.0.0-preview.1.26104.118")]
        public void WhenInstallingASpecificVersionOfWindowsdesktopRuntime(string version)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Don't install windowsdesktop if not on Windows.
                return;
            }

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel: null, "windowsdesktop", quality: null, _sdkInstallationDirectory, version: version);

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().NotHaveStdErr();
            commandResult.Should().HaveStdOutContaining("Installation finished");
            commandResult.Should().Pass();

            // Dotnet CLI is not included in the windowsdesktop runtime. Therefore, version validation cannot be tested.
            // Add the validation once it becomes available in the artifacts.
        }
    }
}
