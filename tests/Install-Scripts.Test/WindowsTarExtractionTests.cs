// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using Microsoft.NET.TestFramework.Assertions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Windows-specific tar extraction tests. Exercises the Extract-Dotnet-Package-Tar
    /// PowerShell function directly with synthetic tarballs, plus format-selection logic
    /// (tar.gz vs zip) via the full install script in dryrun mode.
    /// </summary>
    public class WindowsTarExtractionTests : TarExtractionTestsBase
    {
        private readonly string _scriptPath;

        public WindowsTarExtractionTests(ITestOutputHelper output) : base(output)
        {
            _scriptPath = GetScriptPath();
        }

        protected override void SkipUnlessPrerequisitesMet()
        {
            Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "Windows tar extraction tests are Windows-only.");
            // Intentionally no tar availability check — tar ships with Windows 10+ and
            // these tests should fail loudly if the CI environment is missing it.
        }

        protected override ExtractionResult RunExtraction(string tarPath, string outPath, bool overrideNonVersionedFiles)
        {
            var overrideValue = overrideNonVersionedFiles ? "$true" : "$false";
            var psScript = $@"
$ErrorActionPreference = 'Stop'
$OverrideNonVersionedFiles = {overrideValue}
function Say-Invocation($inv) {{ }}
function Say-Error($msg) {{ Write-Error $msg }}
function Say-Verbose($msg) {{ }}

$scriptContent = Get-Content -Raw '{_scriptPath.Replace("'", "''")}'
if ($scriptContent -match '(?ms)(function Extract-Dotnet-Package-Tar\([^\)]+\)\s*\{{.+?^\}})') {{
    Invoke-Expression $Matches[1]
}} else {{
    throw 'Could not extract function from script'
}}

Extract-Dotnet-Package-Tar -TarPath '{tarPath.Replace("'", "''")}' -OutPath '{outPath.Replace("'", "''")}'
";

            var scriptFilePath = Path.Combine(_testDir, "run-extraction.ps1");
            File.WriteAllText(scriptFilePath, psScript);

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -NoLogo -ExecutionPolicy Bypass -File \"{scriptFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new ExtractionResult(process.ExitCode, stdout, stderr);
        }

        #region Extraction tests (synthetic tarballs)

        /// <summary>
        /// Verifies that hard links in a tarball are correctly resolved during extraction,
        /// resulting in files with the expected content at both the original and linked paths.
        /// </summary>
        [Fact]
        public void HardLinksInTarballAreExtractedCorrectly()
        {
            SkipUnlessPrerequisitesMet();

            var tarPath = CreateTarballWithHardLinks("hardlinks.tar.gz");

            var outPath = Path.Combine(_testDir, "install-hardlinks");
            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);

            result.ExitCode.Should().Be(0, because: $"extraction should succeed.\nStdOut: {result.StdOut}\nStdErr: {result.StdErr}");

            // The original file should exist
            var originalPath = Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0", "original.dll");
            File.Exists(originalPath).Should().BeTrue("original file should be extracted");
            File.ReadAllText(originalPath).Should().Be("shared-content", "original file should have expected content");

            // Hard-linked files should also exist with the same content
            var link1Path = Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0", "link1.dll");
            var link2Path = Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0", "link2.dll");
            var crossDirLinkPath = Path.Combine(outPath, "sdk", "11.0.100", "linked-from-shared.dll");

            File.Exists(link1Path).Should().BeTrue("first hard link should be extracted");
            File.ReadAllText(link1Path).Should().Be("shared-content", "first hard link should have same content as original");

            File.Exists(link2Path).Should().BeTrue("second hard link should be extracted");
            File.ReadAllText(link2Path).Should().Be("shared-content", "second hard link should have same content as original");

            File.Exists(crossDirLinkPath).Should().BeTrue("cross-directory hard link should be extracted");
            File.ReadAllText(crossDirLinkPath).Should().Be("shared-content", "cross-directory hard link should have same content as original");

            // Verify these are actual hard links (link count > 1), not independent copies
            GetHardLinkCount(originalPath).Should().BeGreaterThan(1, "original should be hard-linked");
            GetHardLinkCount(link1Path).Should().BeGreaterThan(1, "link1 should be a hard link");
            GetHardLinkCount(crossDirLinkPath).Should().BeGreaterThan(1, "cross-directory link should be a hard link");
        }

        #endregion

        #region Format selection tests (full script dryrun)

        /// <summary>
        /// Verifies that for .NET 11.0+, the download URL uses .tar.gz extension on Windows
        /// when tar is available, to support archive-level optimizations.
        /// </summary>
        [Theory]
        [InlineData("11.0.100", null)]
        [InlineData("11.0.100", "dotnet")]
        [InlineData("11.0.100", "aspnetcore")]
        [InlineData("12.0.100", null)]
        public void WhenVersionIs11OrHigherOnWindowsUrlUsesTarGz(string version, string? runtime)
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-version", version, "-verbose" };
            if (!string.IsNullOrWhiteSpace(runtime))
            {
                args.Add("-runtime");
                args.Add(runtime);
            }

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutMatching(@"URL\s#0.*\.tar\.gz");
            commandResult.Should().HaveStdOutContaining("Using tar.gz archive format");
        }

        /// <summary>
        /// Verifies that for .NET versions below 11.0, the download URL still uses .zip on Windows.
        /// </summary>
        [Theory]
        [InlineData("9.0.100", null)]
        [InlineData("10.0.100", null)]
        [InlineData("8.0.303", "dotnet")]
        public void WhenVersionIsBelow11OnWindowsUrlUsesZip(string version, string? runtime)
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-version", version, "-verbose" };
            if (!string.IsNullOrWhiteSpace(runtime))
            {
                args.Add("-runtime");
                args.Add(runtime);
            }

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutMatching(@"URL\s#0.*\.zip");
            commandResult.Should().NotHaveStdOutContaining("Using tar.gz archive format");
        }

        /// <summary>
        /// Verifies that 11.0 preview 1 and preview 2 fall back to .zip since
        /// Windows tarballs are only available starting with 11.0 preview 3.
        /// </summary>
        [Theory]
        [InlineData("11.0.100-preview.1.25101.1")]
        [InlineData("11.0.100-preview.2.25201.1")]
        public void WhenVersionIs11Preview1Or2OnWindowsUrlUsesZip(string version)
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-version", version, "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutMatching(@"URL\s#0.*\.zip");
            commandResult.Should().HaveStdOutContaining("predates tar.gz availability");
        }

        /// <summary>
        /// Verifies that windowsdesktop runtime also uses .tar.gz for version >= 11.0.
        /// </summary>
        [Fact]
        public void WhenWindowsDesktopRuntimeVersionIs11OrHigherUrlUsesTarGz()
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-version", "11.0.100", "-runtime", "windowsdesktop", "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutMatching(@"URL\s#0.*\.tar\.gz");
            commandResult.Should().HaveStdOutContaining("Using tar.gz archive format");
        }

        /// <summary>
        /// Verifies that tar availability is detected and reported in verbose output.
        /// </summary>
        [Fact]
        public void TarAvailabilityIsReportedInVerboseOutput()
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutContaining("Tar available:");
        }

        /// <summary>
        /// Verifies that when DOTNET_INSTALL_SKIP_TAR is set, tar is not used
        /// and version 11.0+ falls back to .zip.
        /// </summary>
        [Fact]
        public void WhenSkipTarEnvIsSetUrlUsesZipForVersion11()
        {
            SkipUnlessPrerequisitesMet();

            var envVars = new Dictionary<string, string> { { "DOTNET_INSTALL_SKIP_TAR", "1" } };
            var args = new List<string> { "-dryrun", "-version", "11.0.100", "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args, envVars).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutMatching(@"URL\s#0.*\.zip");
            commandResult.Should().NotHaveStdOutContaining("Using tar.gz archive format");
            commandResult.Should().HaveStdOutContaining("Skipping tar detection due to DOTNET_INSTALL_SKIP_TAR");
        }

        /// <summary>
        /// Verifies that RC versions of .NET 11.0 use .tar.gz (RC is after preview 3).
        /// </summary>
        [Theory]
        [InlineData("11.0.100-rc.1.25401.1")]
        [InlineData("11.0.100-rc.2.25501.1")]
        public void WhenVersionIs11RcOnWindowsUrlUsesTarGz(string version)
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-version", version, "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutMatching(@"URL\s#0.*\.tar\.gz");
            commandResult.Should().HaveStdOutContaining("Using tar.gz archive format");
        }

        /// <summary>
        /// Verifies that 11.0 preview 3+ uses .tar.gz since that is the first preview
        /// with Windows tarball support.
        /// </summary>
        [Theory]
        [InlineData("11.0.100-preview.3.25301.1")]
        [InlineData("11.0.100-preview.4.25401.1")]
        public void WhenVersionIs11Preview3OrLaterOnWindowsUrlUsesTarGz(string version)
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-version", version, "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutMatching(@"URL\s#0.*\.tar\.gz");
            commandResult.Should().HaveStdOutContaining("Using tar.gz archive format");
        }

        /// <summary>
        /// Verifies that channel-based installs (e.g., -Channel 11.0) construct
        /// aka.ms links with .tar.gz when tar is available.
        /// </summary>
        [Theory]
        [InlineData("11.0")]
        public void WhenChannelIs11OrHigherAkaMsLinkUsesTarGz(string channel)
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-channel", channel, "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutContaining(".tar.gz");
        }

        /// <summary>
        /// Verifies that when using a symbolic channel (STS/LTS) with tar available,
        /// the script tries .tar.gz via aka.ms first, then falls back to .zip if needed.
        /// </summary>
        [Fact]
        public void WhenSymbolicChannelUsedAkaMsTriesTarGzFirst()
        {
            SkipUnlessPrerequisitesMet();

            var args = new List<string> { "-dryrun", "-channel", "LTS", "-verbose" };

            var commandResult = TestUtils.CreateInstallCommand(args).ExecuteInstallation();

            commandResult.Should().Pass();
            commandResult.Should().HaveStdOutContaining(".tar.gz");
        }

        /// <summary>
        /// Installs the same .NET version via both .tar.gz and .zip on Windows,
        /// then verifies that the file layouts are identical and that the tarball
        /// installation contains a meaningful number of hard links.
        /// </summary>
        [Fact]
        public void TarGzAndZipInstallationsProduceIdenticalFileLayout()
        {
            SkipUnlessPrerequisitesMet();

            string version = "11.0.100-preview.3.26207.106";

            // Install via .tar.gz (default for 11.0+)
            string tarInstallDir = Path.Combine(_testDir, "tar-install");
            Directory.CreateDirectory(tarInstallDir);
            var tarArgs = new List<string> { "-version", version, "-InstallDir", tarInstallDir, "-verbose" };
            var tarResult = TestUtils.CreateInstallCommand(tarArgs).ExecuteInstallation();

            _output.WriteLine($"Tar install exit code: {tarResult.ExitCode}");
            _output.WriteLine($"Tar install stderr: {tarResult.StdErr}");
            tarResult.Should().Pass();
            tarResult.Should().HaveStdOutContaining("Installation finished");

            // Install via .zip (forced by DOTNET_INSTALL_SKIP_TAR=1)
            string zipInstallDir = Path.Combine(_testDir, "zip-install");
            Directory.CreateDirectory(zipInstallDir);
            var zipEnv = new Dictionary<string, string> { { "DOTNET_INSTALL_SKIP_TAR", "1" } };
            var zipArgs = new List<string> { "-version", version, "-InstallDir", zipInstallDir, "-verbose" };
            var zipResult = TestUtils.CreateInstallCommand(zipArgs, zipEnv).ExecuteInstallation();

            _output.WriteLine($"Zip install exit code: {zipResult.ExitCode}");
            _output.WriteLine($"Zip install stderr: {zipResult.StdErr}");
            zipResult.Should().Pass();
            zipResult.Should().HaveStdOutContaining("Installation finished");

            // Compare file layouts — every file in one should exist in the other
            var tarFiles = GetRelativeFilePaths(tarInstallDir);
            var zipFiles = GetRelativeFilePaths(zipInstallDir);

            tarFiles.Should().BeEquivalentTo(zipFiles,
                "tar.gz and .zip installations should produce identical file layouts");

            _output.WriteLine($"File layout verified: {tarFiles.Count} files match");

            // Count hard links in the tar installation
            int hardLinkCount = CountHardLinks(tarInstallDir);
            _output.WriteLine($"Hard links in tar installation: {hardLinkCount}");
            hardLinkCount.Should().BeGreaterThanOrEqualTo(100,
                "tar.gz installation should contain a significant number of hard links");
        }

        #endregion

        #region Private helpers

        private string CreateTarballWithHardLinks(string name)
        {
            var tarPath = Path.Combine(_testDir, name);
            using var fileStream = File.Create(tarPath);
            using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);
            using var tarWriter = new TarWriter(gzipStream);

            var original = new PaxTarEntry(TarEntryType.RegularFile,
                "shared/Microsoft.NETCore.App/11.0.0/original.dll")
            {
                DataStream = new MemoryStream(Encoding.UTF8.GetBytes("shared-content"))
            };
            tarWriter.WriteEntry(original);

            var link1 = new PaxTarEntry(TarEntryType.HardLink,
                "shared/Microsoft.NETCore.App/11.0.0/link1.dll")
            {
                LinkName = "shared/Microsoft.NETCore.App/11.0.0/original.dll"
            };
            tarWriter.WriteEntry(link1);

            var link2 = new PaxTarEntry(TarEntryType.HardLink,
                "shared/Microsoft.NETCore.App/11.0.0/link2.dll")
            {
                LinkName = "shared/Microsoft.NETCore.App/11.0.0/original.dll"
            };
            tarWriter.WriteEntry(link2);

            var crossDirLink = new PaxTarEntry(TarEntryType.HardLink,
                "sdk/11.0.100/linked-from-shared.dll")
            {
                LinkName = "shared/Microsoft.NETCore.App/11.0.0/original.dll"
            };
            tarWriter.WriteEntry(crossDirLink);

            return tarPath;
        }

        private static string GetScriptPath()
        {
            string? directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, ".git")))
            {
                directory = Directory.GetParent(directory)?.FullName;
            }
            return Path.Combine(directory ?? ".", "src", "dotnet-install.ps1");
        }

        private static SortedSet<string> GetRelativeFilePaths(string rootDir)
        {
            var relativePaths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories))
            {
                relativePaths.Add(Path.GetRelativePath(rootDir, file));
            }
            return relativePaths;
        }

        private int CountHardLinks(string rootDir)
        {
            int count = 0;
            foreach (var file in Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories))
            {
                if (GetHardLinkCount(file) > 1)
                {
                    count++;
                }
            }
            return count;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetFileInformationByHandle(
            IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode,
            IntPtr lpSecurityAttributes, uint dwCreationDisposition,
            uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        private struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        private static uint GetHardLinkCount(string filePath)
        {
            const uint GENERIC_READ = 0x80000000;
            const uint FILE_SHARE_READ = 0x00000001;
            const uint OPEN_EXISTING = 3;
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            IntPtr handle = CreateFile(filePath, GENERIC_READ, FILE_SHARE_READ,
                IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
            if (handle == INVALID_HANDLE_VALUE)
                return 1;

            try
            {
                if (GetFileInformationByHandle(handle, out BY_HANDLE_FILE_INFORMATION info))
                {
                    return info.NumberOfLinks;
                }
                return 1;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        #endregion
    }
}
