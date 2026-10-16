// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class PackageDetectionTests : IDisposable
    {
        private const string Version = "10.0.100";
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private readonly string _testDir = Path.Combine(Path.GetTempPath(), nameof(PackageDetectionTests), Path.GetRandomFileName());

        public PackageDetectionTests()
        {
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose() => Directory.Delete(_testDir, recursive: true);

        [Theory]
        [InlineData("sdk", "missing", false)]
        [InlineData("sdk", "file", true)]
        [InlineData("sdk", "directory", false)]
        [InlineData("shared/Microsoft.NETCore.App", "missing", false)]
        [InlineData("shared/Microsoft.NETCore.App", "file", true)]
        [InlineData("shared/Microsoft.NETCore.App", "directory", false)]
        [InlineData("shared/Microsoft.AspNetCore.App", "missing", false)]
        [InlineData("shared/Microsoft.AspNetCore.App", "file", true)]
        [InlineData("shared/Microsoft.AspNetCore.App", "directory", false)]
        public void InstalledPackageRequiresDotnetHost(string relativePath, string hostKind, bool expected)
        {
            Directory.CreateDirectory(Path.Combine(_testDir, relativePath, Version));
            string hostPath = Path.Combine(_testDir, IsWindows ? "dotnet.exe" : "dotnet");
            if (hostKind == "file")
            {
                File.WriteAllText(hostPath, "host");
            }
            else if (hostKind == "directory")
            {
                Directory.CreateDirectory(hostPath);
            }

            Assert.Equal(expected, IsPackageInstalled(relativePath));
        }

        [Fact]
        public void DotnetHostWithoutPackageIsNotInstalled()
        {
            File.WriteAllText(Path.Combine(_testDir, IsWindows ? "dotnet.exe" : "dotnet"), "host");

            Assert.False(IsPackageInstalled("sdk"));
        }

        [Fact]
        public void WindowsDesktopRuntimeDoesNotRequireDotnetHost()
        {
            Assert.SkipUnless(IsWindows, "The Windows Desktop runtime is Windows-only.");
            const string relativePath = "shared/Microsoft.WindowsDesktop.App";
            Directory.CreateDirectory(Path.Combine(_testDir, relativePath, Version));

            Assert.True(IsPackageInstalled(relativePath));
        }

        private bool IsPackageInstalled(string relativePath)
        {
            string? repoRoot = AppContext.BaseDirectory;
            while (repoRoot != null &&
                   !Directory.Exists(Path.Combine(repoRoot, ".git")) &&
                   !File.Exists(Path.Combine(repoRoot, ".git")))
            {
                repoRoot = Directory.GetParent(repoRoot)?.FullName;
            }
            Assert.NotNull(repoRoot);

            string extension = IsWindows ? "ps1" : "sh";
            string source = File.ReadAllText(Path.Combine(repoRoot, "src", "dotnet-install." + extension));
            string script;
            if (IsWindows)
            {
                script = "param($InstallRoot, $RelativePath, $Version)\n" +
                    "$ErrorActionPreference = 'Stop'\n" +
                    "function Say-Invocation($invocation) {}\n" +
                    "function Say-Verbose($message) {}\n" +
                    ExtractFunction(source, @"function Is-Dotnet-Package-Installed\(") + "\n" +
                    "Is-Dotnet-Package-Installed $InstallRoot $RelativePath $Version\n";
            }
            else
            {
                script = "set -eu\ninvocation=':'\nsay_verbose() { :; }\n";
                foreach (string name in new[] { "remove_trailing_slash", "remove_beginning_slash", "combine_paths", "is_dotnet_package_installed" })
                {
                    script += ExtractFunction(source, name + @"\(\)") + "\n";
                }
                script += "if is_dotnet_package_installed \"$1\" \"$2\" \"$3\"; then echo True; else echo False; fi\n";
            }

            string scriptPath = Path.Combine(_testDir, "detect-package." + extension);
            File.WriteAllText(scriptPath, script.Replace("\r\n", "\n"));
            var startInfo = new ProcessStartInfo(IsWindows ? "powershell" : "/bin/bash")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            if (IsWindows)
            {
                foreach (string argument in new[] { "-NoProfile", "-NoLogo", "-ExecutionPolicy", "Bypass", "-File" })
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }
            foreach (string argument in new[] { scriptPath, _testDir, relativePath, Version })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.True(process.ExitCode == 0, $"StdOut: {stdout}\nStdErr: {stderr}");
            return bool.Parse(stdout.Trim());
        }

        private static string ExtractFunction(string source, string declaration)
        {
            var match = Regex.Match(source, "(?ms)^" + declaration + ".+?^}");
            Assert.True(match.Success, $"Could not find function matching '{declaration}'.");
            return match.Value;
        }
    }
}
