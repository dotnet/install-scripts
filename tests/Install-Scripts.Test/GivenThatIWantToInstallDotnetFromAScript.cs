// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using Install_Scripts.Test.Utils;
using Microsoft.NET.TestFramework.Assertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class GivenThatIWantToInstallDotnetFromAScript : IDisposable
    {

        /// <summary>
        /// All the channels that will be tested.
        /// </summary>
        private static readonly IReadOnlyList<(string channel, string versionRegex, Quality quality)> _channels =
            new List<(string, string, Quality)>()
            {
                ("2.1", "2\\.1\\..*", Quality.None),
                ("2.2", "2\\.2\\..*", Quality.None),
                ("3.0", "3\\.0\\..*", Quality.None),
                ("3.1", "3\\.1\\..*", Quality.None),
                ("5.0", "5\\.0\\..*", Quality.None),
                ("6.0", "6\\.0\\..*", Quality.Daily),
                ("6.0", "6\\.0\\..*", Quality.None),
                ("7.0", "7\\.0\\..*", Quality.None),
                ("7.0", "7\\.0\\..*", Quality.Ga),
                ("8.0", "8\\.0\\..*", Quality.None),
                ("8.0", "8\\.0\\..*", Quality.Ga),
                ("STS", "9\\.0\\..*", Quality.None),
                ("9.0", "9\\.0\\..*", Quality.None),
                ("9.0", "9\\.0\\..*", Quality.Ga),
                ("LTS", "10\\.0\\..*", Quality.None),
                ("10.0", "10\\.0\\..*", Quality.None),
                ("10.0", "10\\.0\\..*", Quality.Ga),
            };

        /// <summary>
        /// All the branches in runtime repos to be tested
        /// </summary>
        private static readonly IReadOnlyList<(string branch, string versionRegex, Quality quality)> _runtimeBranches =
            new List<(string, string, Quality)>()
            {
                ("release/2.1", "2\\.1\\..*", Quality.None),
                ("release/2.2", "2\\.2\\..*", Quality.None),
                ("release/3.0", "3\\.0\\..*", Quality.None),
                ("release/3.1", "3\\.1\\..*", Quality.None),
                // ("release/5.0", "5\\.0\\..*", Quality.None), Broken scenario
                // Branches are no longer supported starting 6.0, but there are channels that correspond to branches.
                // this storage account does not allow public access
                // ("6.0-preview2", "6\\.0\\..*", Quality.Daily | Quality.Signed),
                ("6.0-preview3", "6\\.0\\..*", Quality.Daily),
                ("6.0-preview4", "6\\.0\\..*", Quality.Daily),
                ("6.0", "6\\.0\\..*", Quality.None),
                ("7.0", "7\\.0\\..*", Quality.None),
                ("8.0", "8\\.0\\..*", Quality.None),
                ("9.0", "9\\.0\\..*", Quality.None),
                ("10.0", "10\\.0\\..*", Quality.None),
            };

        /// <summary>
        /// All the branches in installer repo to be tested
        /// </summary>
        private static readonly IReadOnlyList<(string branch, string versionRegex, Quality quality)> _sdkBranches =
            new List<(string, string, Quality)>()
            {
                ("release/2.1.8xx", "2\\.1\\.8.*", Quality.None),
                ("release/2.2.4xx", "2\\.2\\.4.*", Quality.None),
                ("release/3.0.1xx", "3\\.0\\.1.*", Quality.None),
                // version is outdated. For more details check the link: https://github.com/dotnet/arcade/issues/10026
                // ("release/3.1.4xx", "3\\.1\\.4.*", Quality.None),
                ("release/5.0.1xx", "5\\.0\\.1.*", Quality.None),
                ("release/5.0.2xx", "5\\.0\\.2.*", Quality.None),
                // Branches are no longer supported starting 6.0, but there are channels that correspond to branches.
                // this storage account does not allow public access
                // ("6.0.1xx-preview2", "6\\.0\\.1.*", Quality.Daily | Quality.Signed),
                ("6.0.1xx-preview3", "6\\.0\\.1.*", Quality.Daily),
                ("6.0.1xx-preview4", "6\\.0\\.1.*", Quality.Daily),
                ("7.0.1xx", "7\\.0\\..*", Quality.Daily),
                ("8.0.1xx", "8\\.0\\..*", Quality.Daily),
                ("9.0.1xx", "9\\.0\\..*", Quality.Daily),
                ("10.0.1xx", "10\\.0\\..*", Quality.Preview),
            };

        public static IEnumerable<object?[]> InstallSdkFromChannelTestCases
        {
            get
            {
                // Download SDK using branches as channels.
                foreach (var sdkBranchInfo in _sdkBranches)
                {
                    foreach (string? quality in GetQualityOptionsFromFlags(sdkBranchInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            sdkBranchInfo.branch,
                            quality,
                            sdkBranchInfo.versionRegex,
                        };
                    }
                }

                // Download SDK from darc channels.
                foreach (var channelInfo in _channels)
                {
                    foreach(string? quality in GetQualityOptionsFromFlags(channelInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            channelInfo.channel,
                            quality,
                            channelInfo.versionRegex,
                        };
                    }
                }
            }
        }

        public static IEnumerable<object?[]> InstallRuntimeFromChannelTestCases
        {
            get
            {
                // Download runtimes using branches as channels.
                foreach (var runtimeBranchInfo in _runtimeBranches)
                {
                    foreach (string? quality in GetQualityOptionsFromFlags(runtimeBranchInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            runtimeBranchInfo.branch,
                            quality,
                            runtimeBranchInfo.versionRegex,
                        };
                    }
                }

                // Download runtimes using darc channels.
                foreach (var channelInfo in _channels)
                {
                    foreach (string? quality in GetQualityOptionsFromFlags(channelInfo.quality).DefaultIfEmpty())
                    {
                        yield return new object?[]
                        {
                            channelInfo.channel,
                            quality,
                            channelInfo.versionRegex,
                        };
                    }
                }
            }
        }

        /// <summary>
        /// The directory that the install script will install the .NET into.
        /// </summary>
        private readonly string _sdkInstallationDirectory;

        private readonly ITestOutputHelper outputHelper;

        /// <summary>
        /// Instantiates a GivenThatIWantToInstallTheSdkFromAScript instance.
        /// </summary>
        /// <remarks>This constructor is called once for each of the tests to run.</remarks>
        public GivenThatIWantToInstallDotnetFromAScript(ITestOutputHelper testOutputHelper)
        {
            outputHelper = testOutputHelper;

            _sdkInstallationDirectory = Path.Combine(
                Path.GetTempPath(),
                "InstallScript-Tests",
                Path.GetRandomFileName());

            // In case there are any files from previous runs, clean them up.
            try
            {
                Directory.Delete(_sdkInstallationDirectory, true);
            }
            catch (DirectoryNotFoundException)
            {
                // This is expected. Ignore the exception.
            }

            Directory.CreateDirectory(_sdkInstallationDirectory);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        /// <remarks>This method is called after each test. It is used for cleaning
        /// the leftover files from each test run.</remarks>
        public void Dispose()
        {
            try
            {
                Directory.Delete(_sdkInstallationDirectory, true);
            }
            catch (DirectoryNotFoundException)
            {
                // Directory to cleanup may not be there if installation fails. Not an issue. Ignore the exception.
            }
            catch (UnauthorizedAccessException e)
            {
                throw new Exception($"Failed to remove {_sdkInstallationDirectory}", e);
            }
        }

        [Theory]
        [MemberData(nameof(InstallSdkFromChannelTestCases))]
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
        [MemberData(nameof(InstallRuntimeFromChannelTestCases))]
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
        [MemberData(nameof(InstallRuntimeFromChannelTestCases))]
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
        [MemberData(nameof(InstallRuntimeFromChannelTestCases))]
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
            // Add the validation once the becomes available in the artifacts.
        }

        [Theory]
        [MemberData(nameof(InstallSdkFromChannelTestCases))]
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
        [MemberData(nameof(InstallRuntimeFromChannelTestCases))]
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
        [InlineData("5.0.404-servicing.21560.14", "5.0.404")]
        [InlineData("6.0.100-preview.6.21364.34")]
        [InlineData("7.0.100-alpha.1.22054.9")]
        [InlineData("8.0.404")]
        [InlineData("9.0.100")]
        [InlineData("10.0.100")]
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
        [InlineData("5.0.13-servicing.21560.6", "5.0.13")]
        [InlineData("6.0.0-preview.4.21176.7")]
        [InlineData("7.0.0-alpha.1.21528.8")]
        [InlineData("8.0.11")]
        [InlineData("9.0.0")]
        [InlineData("10.0.0")]
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
            // Add the validation once the becomes available in the artifacts.
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

        private static IEnumerable<string> GetInstallScriptArgs(
            string? channel,
            string? runtime,
            string? quality,
            string? installDir,
            string? feedCredentials = null,
            bool verboseLogging = false,
            string? version = null)
        {
            if (!string.IsNullOrWhiteSpace(channel))
            {
                yield return "-Channel";
                yield return channel;
            }

            if (!string.IsNullOrWhiteSpace(installDir))
            {
                yield return "-InstallDir";
                yield return installDir;
            }

            if (!string.IsNullOrWhiteSpace(runtime))
            {
                yield return "-Runtime";
                yield return runtime;
            }

            if (!string.IsNullOrWhiteSpace(quality))
            {
                yield return "-Quality";
                yield return quality;
            }

            if (!string.IsNullOrWhiteSpace(feedCredentials))
            {
                yield return "-FeedCredential";
                yield return feedCredentials;
            }

            if (!string.IsNullOrWhiteSpace(version))
            {
                yield return "-Version";
                yield return version;
            }

            if (verboseLogging)
            {
                yield return "-Verbose";
            }
        }

        private static IEnumerable<string> GetQualityOptionsFromFlags(Quality flags)
        {
            ulong flagsValue = (ulong)flags;

            if(flagsValue == 0)
            {
                yield break;
            }

            foreach (Quality quality in Enum.GetValues(typeof(Quality)))
            {
                ulong qualityValue = (ulong)quality;
                if(qualityValue == 0 || (qualityValue & (qualityValue-1)) != 0)
                {
                    // No bits are set, or more than one bits are set
                    continue;
                }

                if ((flagsValue & qualityValue) != 0)
                {
                    yield return quality.ToString();
                }
            }
        }
    }
}
