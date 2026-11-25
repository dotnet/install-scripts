// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Install_Scripts.Test.Utils;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Tests for OpenHarmony/HarmonyOS OS detection in dotnet-install.sh.
    /// Unix-only because the script under test is the bash variant.
    /// </summary>
    public class OpenHarmonyOsDetectionTests
    {
        [Fact]
        public void WhenTheMachineReportsHarmonyOS_TheLinkUsesTheOpenHarmonyRid()
        {
            Assert.SkipWhen(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "dotnet-install.sh tests are Unix-only.");

            using var fixture = new OsDetectionFixture();

            CommandResult result = fixture.Run(
                "-dryrun",
                "-version", "11.0.100",
                "-azurefeed", "file://" + fixture.FeedPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("dotnet-sdk-11.0.100-openharmony-arm64.tar.gz", result.StdOut);
        }

        [Theory]
        [InlineData("harmonyos")]
        [InlineData("ohos")]
        [InlineData("openharmony")]
        public void WhenAnOpenHarmonyOsAliasIsPassed_TheLinkUsesTheOpenHarmonyRid(string osAlias)
        {
            Assert.SkipWhen(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "dotnet-install.sh tests are Unix-only.");

            using var fixture = new OsDetectionFixture();

            CommandResult result = fixture.Run(
                "-dryrun",
                "-version", "11.0.100",
                "-os", osAlias,
                "-azurefeed", "file://" + fixture.FeedPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("openharmony-arm64", result.StdOut);
        }

        private sealed class OsDetectionFixture : IDisposable
        {
            private readonly string _root;
            private readonly string _shimDir;

            public OsDetectionFixture()
            {
                _root = Path.Combine(Path.GetTempPath(), "dotnet-ohos-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_root);

                // Override uname so the script sees HarmonyOS regardless of the test host.
                _shimDir = Path.Combine(_root, "shim");
                Directory.CreateDirectory(_shimDir);
                string unamePath = Path.Combine(_shimDir, "uname");
                File.WriteAllText(unamePath, "#!/bin/sh\ncase \"${1:-}\" in\n  -m) echo aarch64 ;;\n  -s) echo HarmonyOS ;;\n  *) echo HarmonyOS ;;\nesac\n");
                File.SetUnixFileMode(unamePath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

                FeedPath = Path.Combine(_root, "feed");
                string versionDir = Path.Combine(FeedPath, "Sdk", "11.0.100");
                Directory.CreateDirectory(versionDir);
                File.WriteAllText(Path.Combine(versionDir, "productVersion.txt"), "11.0.100\n");
            }

            public string FeedPath { get; }

            public CommandResult Run(params string[] args)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"{GetScriptPath()} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args)}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                startInfo.Environment["PATH"] = _shimDir + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
                startInfo.Environment["TMPDIR"] = _root;

                using Process process = Process.Start(startInfo)!;
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new CommandResult(process.ExitCode, stdout, stderr);
            }

            private static string GetScriptPath()
            {
                string? directory = AppContext.BaseDirectory;
                while (directory != null && !Directory.Exists(Path.Combine(directory, ".git")))
                {
                    directory = Directory.GetParent(directory)?.FullName;
                }

                if (directory == null)
                {
                    throw new InvalidOperationException("Could not locate the repository root.");
                }

                return Path.Combine(directory, "src", "dotnet-install.sh");
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(_root, true);
                }
                catch
                {
                    // Best effort cleanup.
                }
            }
        }

        private sealed record CommandResult(int ExitCode, string StdOut, string StdErr);
    }
}
