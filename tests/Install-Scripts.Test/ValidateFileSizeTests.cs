// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class ValidateFileSizeTests : IDisposable
    {
        private readonly string _tempDir;

        public ValidateFileSizeTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "InstallScript-FileSizeTests", Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); }
            catch (DirectoryNotFoundException) { }
        }

        [Fact]
        public void WhenRemoteSizeIsUnavailable_ShouldNotWarnAboutCorruption()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            var tempFile = CreateTempFileWithSize(1024);
            var result = RunPowerShellValidation(tempFile, remoteFileSize: null);

            result.StdOut.Should().Contain("Downloaded file");
            result.StdOut.Should().Contain("1024 bytes");
            result.StdOut.Should().NotContain("corrupted");
            result.StdOut.Should().Contain("Skipping file size validation");
        }

        [Fact]
        public void WhenFileSizesMatch_ShouldReportEqual()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            var tempFile = CreateTempFileWithSize(2048);
            var result = RunPowerShellValidation(tempFile, remoteFileSize: "2048");

            result.StdOut.Should().Contain("remote and local file sizes are equal");
            result.StdOut.Should().NotContain("corrupted");
        }

        [Fact]
        public void WhenFileSizesDontMatch_ShouldWarnAboutCorruption()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            var tempFile = CreateTempFileWithSize(1024);
            var result = RunPowerShellValidation(tempFile, remoteFileSize: "9999");

            result.StdOut.Should().Contain("remote and local file sizes are not equal");
            result.StdOut.Should().Contain("may be corrupted");
        }

        [Fact]
        public void WhenLocalFileIsMissing_ShouldWarnAboutCorruptionOrMissing()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            var missingFile = Path.Combine(_tempDir, "does-not-exist.zip");
            var result = RunPowerShellValidation(missingFile, remoteFileSize: "1024");

            result.StdOut.Should().Contain("corrupted or missing");
        }

        [Fact]
        public void WhenExceptionOccurs_ShouldNotWarnAboutCorruption()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

            // Inject a Get-Remote-File-Size that throws to exercise the catch block.
            var tempFile = CreateTempFileWithSize(512);
            string escapedPath = tempFile.Replace("'", "''");
            string script = $@"
function Say($str) {{ Write-Host ""dotnet-install: $str"" }}
function Say-Verbose($str) {{ Write-Host ""dotnet-install: $str"" }}
function Get-Remote-File-Size($zipUri) {{ throw 'Simulated network error' }}

{GetValidateFunctionSource()}

ValidateRemoteLocalFileSizes -LocalFileOutPath '{escapedPath}' -SourceUri 'https://example.com/test.zip'
";

            var result = RunPowerShellScript(script);

            result.StdOut.Should().NotContain("One of them may be corrupted");
            result.StdOut.Should().Contain("Unable to validate");
        }

        private string CreateTempFileWithSize(int sizeInBytes)
        {
            var filePath = Path.Combine(_tempDir, Path.GetRandomFileName());
            File.WriteAllBytes(filePath, new byte[sizeInBytes]);
            return filePath;
        }

        private (string StdOut, string StdErr, int ExitCode) RunPowerShellValidation(
            string localFilePath,
            string? remoteFileSize)
        {
            string mockRemoteReturn = remoteFileSize != null
                ? $"return '{remoteFileSize}'"
                : "return $null";

            string escapedPath = localFilePath.Replace("'", "''");

            string script = $@"
function Say($str) {{ Write-Host ""dotnet-install: $str"" }}
function Say-Verbose($str) {{ Write-Host ""dotnet-install: $str"" }}
function Get-Remote-File-Size($zipUri) {{ {mockRemoteReturn} }}

{GetValidateFunctionSource()}

ValidateRemoteLocalFileSizes -LocalFileOutPath '{escapedPath}' -SourceUri 'https://example.com/test.zip'
";
            return RunPowerShellScript(script);
        }

        private static string GetValidateFunctionSource() => @"
function ValidateRemoteLocalFileSizes([string]$LocalFileOutPath, $SourceUri) {
    try {
        $remoteFileSize = Get-Remote-File-Size -zipUri $SourceUri
        $localFileSize = $null

        if (Test-Path $LocalFileOutPath) {
            $localFileSize = [long](Get-Item $LocalFileOutPath).Length
            Say ""Downloaded file $SourceUri size is $localFileSize bytes.""
        }

        if ($null -eq $localFileSize -or $localFileSize -le 0) {
            Say ""Local file size could not be measured. The package may be corrupted or missing.""
            return
        }

        if ([string]::IsNullOrEmpty($remoteFileSize)) {
            Say-Verbose ""Remote file size could not be determined. Skipping file size validation.""
            return
        }

        if ($remoteFileSize -ne $localFileSize) {
            Say ""The remote and local file sizes are not equal. Remote file size is $remoteFileSize bytes and local size is $localFileSize bytes. The local package may be corrupted.""
        }
        else {
            Say ""The remote and local file sizes are equal.""
        }
    }
    catch {
        Say-Verbose ""Unable to validate remote and local file sizes.""
    }
}
";

        private static (string StdOut, string StdErr, int ExitCode) RunPowerShellScript(string script)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-ExecutionPolicy Bypass -NoProfile -NoLogo -Command -",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.StandardInput.Write(script);
            process.StandardInput.Close();

            string stdOut = process.StandardOutput.ReadToEnd();
            string stdErr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return (stdOut, stdErr, process.ExitCode);
        }
    }
}
