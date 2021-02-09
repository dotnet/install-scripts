// Copyright (c) Microsoft. All rights reserved.
#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Xunit;
using System.Collections.Generic;
using Microsoft.NET.TestFramework.Assertions;
using FluentAssertions;
using System.Text.RegularExpressions;
using System.Linq;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class GivenThatIWantToInstallDotnetFromAScript : IDisposable
    {
        /// <summary>
        /// All the channels that will be tested.
        /// </summary>
        private static readonly IReadOnlyList<(string channel, string versionRegex)> _channels =
            new List<(string, string)>()
            {
                ("2.1", "2\\.1\\..*"),
                ("2.2", "2\\.2\\..*"),
                ("3.0", "3\\.0\\..*"),
                ("3.1", "3\\.1\\..*"),
                ("5.0", "5\\.0\\..*"),
                ("Current", "5\\.0\\..*"),
                ("LTS", "3\\.1\\..*"),
            };

        /// <summary>
        /// All the branches in runtime repos to be tested
        /// </summary>
        private static readonly IReadOnlyList<(string branch, string versionRegex)> _runtimeBranches =
            new List<(string, string)>()
            {
                ("release/2.1", "2\\.1\\..*"),
                ("release/2.2", "2\\.2\\..*"),
                ("release/3.0", "3\\.0\\..*"),
                ("release/3.1", "3\\.1\\..*"),
                ("release/5.0", "5\\.0\\..*"),
            };

        /// <summary>
        /// All the branches in installer repo to be tested
        /// </summary>
        private static readonly IReadOnlyList<(string branch, string versionRegex)> _sdkBranches =
            new List<(string, string)>()
            {
                ("release/2.1.8xx", "2\\.1\\.8.*"),
                ("release/2.2.4xx", "2\\.2\\.4.*"),
                ("release/3.0.1xx", "3\\.0\\.1.*"),
                ("release/3.1.4xx", "3\\.1\\.4.*"),
                ("release/5.0.1xx", "5\\.0\\.1.*"),
                ("release/5.0.2xx", "5\\.0\\.2.*"),
                ("master", "6\\.0\\.1.*"),
            };

        public static IEnumerable<object?[]> InstallSdkFromChannelTestCases
        {
            get
            {
                // Download SDK using branches as channels.
                foreach (var sdkBranchInfo in _sdkBranches)
                {
                    yield return new object?[]
                    {
                        sdkBranchInfo.branch,
                        sdkBranchInfo.versionRegex,
                    };
                }

                // Download SDK from darc channels.
                foreach (var channelInfo in _channels)
                {
                    yield return new object?[]
                    {
                        channelInfo.channel,
                        channelInfo.versionRegex,
                    };
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
                    yield return new object?[]
                    {
                        runtimeBranchInfo.branch,
                        runtimeBranchInfo.versionRegex,
                    };
                }

                // Download runtimes using darc channels.
                foreach (var channelInfo in _channels)
                {
                    yield return new object?[]
                    {
                        channelInfo.channel,
                        channelInfo.versionRegex,
                    };
                }
            }
        }

        /// <summary>
        /// The directory that the install script will install the .NET into.
        /// </summary>
        private readonly string _sdkInstallationDirectory;

        /// <summary>
        /// Instantiates a GivenThatIWantToInstallTheSdkFromAScript instance.
        /// </summary>
        /// <remarks>This constructor is called once for each of the tests to run.</remarks>
        public GivenThatIWantToInstallDotnetFromAScript()
        {
            _sdkInstallationDirectory = Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName(),
                "InstallScript-Tests");

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
        }

        [Theory]
        [MemberData(nameof(InstallSdkFromChannelTestCases))]
        public void WhenInstallingTheSdk(string channel, string versionRegex)
        {
            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, null, _sdkInstallationDirectory);

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().HaveStdOutContaining("Installation finished");

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = CreateDotnetCommand(_sdkInstallationDirectory, dotnetArgs)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();

            string installPath = " [" + Path.Combine(_sdkInstallationDirectory, "sdk") + "]";
            string regex = Regex.Escape("  ") + versionRegex + Regex.Escape(installPath);
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
        }

        [Theory]
        [MemberData(nameof(InstallRuntimeFromChannelTestCases))]
        public void WhenInstallingDotnetRuntime(string channel, string versionRegex)
        {
            if (channel == "release/5.0")
            {
                // Broken scenario
                return;
            }

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, "dotnet", _sdkInstallationDirectory);

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().HaveStdOutContaining("Installation finished");

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = CreateDotnetCommand(_sdkInstallationDirectory, dotnetArgs)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();

            string lineStart = " Microsoft.NETCore.App ";
            string lineEnd = " [" + Path.Combine(_sdkInstallationDirectory, "shared", "Microsoft.NETCore.App") + "]";
            string regex = Regex.Escape(lineStart) + versionRegex + Regex.Escape(lineEnd);
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
        }

        [Theory]
        [MemberData(nameof(InstallRuntimeFromChannelTestCases))]
        public void WhenInstallingAspNetCoreRuntime(string channel, string versionRegex)
        {
            if (channel == "release/3.0"
                || channel == "release/3.1"
                || channel == "release/5.0")
            {
                // These scenarios are broken.
                return;
            }
            
            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, "aspnetcore", _sdkInstallationDirectory);

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().HaveStdOutContaining("Installation finished");

            // Run dotnet to verify that the version is installed into correct folder.
            var dotnetArgs = new List<string> { "--info" };

            var dotnetCommandResult = CreateDotnetCommand(_sdkInstallationDirectory, dotnetArgs)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute();

            string lineStart = " Microsoft.AspNetCore.App ";
            string lineEnd = " [" + Path.Combine(_sdkInstallationDirectory, "shared", "Microsoft.AspNetCore.App") + "]";
            string regex = Regex.Escape(lineStart) + versionRegex + Regex.Escape(lineEnd);
            dotnetCommandResult.Should().HaveStdOutMatching(regex);
        }

        [Theory]
        [MemberData(nameof(InstallRuntimeFromChannelTestCases))]
        public void WhenInstallingWindowsdesktopRuntime(string channel, string versionRegex)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Don't install windowsdesktop if not on Windows.
                return;
            }

            List<Regex> exclusions = new List<Regex>()
            {
                new Regex(".*2\\..*"), // Runtime is not supported in this version.
                new Regex(".*3\\.0.*"), // Runtime is not supported in this version.
                new Regex("release/3.1"), // Broken scenario.
                new Regex("release/5.0"), // Broken scenario.
            };

            if (exclusions.Any(e => e.IsMatch(channel)))
            {
                // Test is excluded.
                return;
            }

            // Run install script to download and install.
            var args = GetInstallScriptArgs(channel, "windowsdesktop", _sdkInstallationDirectory);

            var commandResult = CreateInstallCommand(args)
                            .CaptureStdOut()
                            .CaptureStdErr()
                            .Execute();

            commandResult.Should().HaveStdOutContaining("Installation finished");

            // Dotnet CLI is not included in the windowsdesktop runtime. Therefore, version validation cannot be tested.
            // Add the validation once the becomes available in the artifacts.
        }

        private static IEnumerable<string> GetInstallScriptArgs(string? channel, string? runtime, string? installDir)
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
        }

        private static Command CreateDotnetCommand(string workingDirectory, IEnumerable<string> args)
        {
            string path;
            string finalArgs;

            path = Path.Combine(workingDirectory, "dotnet");
            finalArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args);

            return Command.Create(new CommandSpec(path, finalArgs, CommandResolutionStrategy.None));
        }

        private static Command CreateInstallCommand(IEnumerable<string> args)
        {
            string path;
            string finalArgs;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = "powershell.exe";
                finalArgs = "-ExecutionPolicy Bypass -NoProfile -NoLogo -Command \"" +
                    Path.Combine(GetRepoRoot(), "src", "dotnet-install.ps1") + " " + ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args) + "\"";
            }
            else
            {
                path = Path.Combine(GetRepoRoot(), "src", "dotnet-install.sh");
                finalArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args);
            }

            return Command.Create(new CommandSpec(path, finalArgs, CommandResolutionStrategy.None));
        }

        private static string GetRepoRoot()
        {
            string directory = AppContext.BaseDirectory;

            while (!Directory.Exists(Path.Combine(directory, ".git")) && directory != null)
            {
                directory = Directory.GetParent(directory)?.FullName;
            }

            if (directory == null)
            {
                return null;
            }
            return directory;
        }
    }
}