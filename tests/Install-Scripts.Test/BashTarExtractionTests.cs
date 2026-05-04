// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using System;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Tests that exercise the extract_dotnet_package function from dotnet-install.sh
    /// using synthetic tarballs. Runs on Unix only. Includes symlink preservation tests
    /// specific to the Linux/macOS extraction path.
    /// </summary>
    public class BashTarExtractionTests : TarExtractionTestsBase
    {
        private readonly string _scriptPath;

        public BashTarExtractionTests(ITestOutputHelper output) : base(output)
        {
            _scriptPath = GetScriptPath();
        }

        protected override void SkipUnlessPrerequisitesMet()
        {
            Assert.SkipWhen(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "Bash tar extraction tests are Unix-only.");
        }

        protected override ExtractionResult RunExtraction(string tarPath, string outPath, bool overrideNonVersionedFiles)
        {
            var overrideValue = overrideNonVersionedFiles ? "true" : "false";
            var script = $@"
set -e

# Minimal stubs for functions called by extract_dotnet_package
say_verbose() {{ :; }}
say_err() {{ echo ""ERROR: $*"" >&2; }}
eval_invocation=""return""
invocation="":""

override_non_versioned_files={overrideValue}
temporary_file_template=""{Path.Combine(_testDir, "dotnet-install.XXXXXX").Replace("\"", "\\\"")}""

# Define helper functions inline
remove_trailing_slash() {{
    local input=""${{1:-}}""
    echo ""${{input%/}}""
}}

remove_beginning_slash() {{
    local input=""${{1:-}}""
    echo ""${{input#/}}""
}}

get_cp_options() {{
    local override=""$1""
    if [ ""$override"" = false ]; then
        echo ""-n""
    else
        echo """"
    fi
}}

validate_remote_local_file_sizes() {{
    :
}}

# Source the copy and extract functions from the script
source_funcs() {{
    local script_content
    script_content=$(cat '{_scriptPath.Replace("'", "'\\''")}')
    
    # Extract copy_files_or_dirs_from_list function  
    eval ""$(echo ""$script_content"" | sed -n '/^copy_files_or_dirs_from_list()/,/^}}/p')""
    
    # Extract extract_dotnet_package function
    eval ""$(echo ""$script_content"" | sed -n '/^extract_dotnet_package()/,/^}}/p')""
}}

source_funcs

# Run extraction
extract_dotnet_package '{tarPath.Replace("'", "'\\''")}' '{outPath.Replace("'", "'\\''")}' """"
";
            return RunBash(script);
        }

        /// <summary>
        /// Verifies that symbolic links within versioned directories are preserved
        /// after extraction through the bash script's extract_dotnet_package function.
        /// </summary>
        [Fact]
        public void SymlinksInVersionedDirectoriesArePreserved()
        {
            SkipUnlessPrerequisitesMet();

            string tarPath = CreateTarballWithSymlinks();
            string outPath = Path.Combine(_testDir, "out");
            Directory.CreateDirectory(outPath);

            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);
            _output.WriteLine($"Exit code: {result.ExitCode}");
            _output.WriteLine($"Stdout: {result.StdOut}");
            _output.WriteLine($"Stderr: {result.StdErr}");
            result.ExitCode.Should().Be(0, $"extraction should succeed.\nStdout: {result.StdOut}\nStderr: {result.StdErr}");

            // The original file should exist
            string originalFile = Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0", "original.dll");
            File.Exists(originalFile).Should().BeTrue("the original file should be extracted");

            // The symlink in the versioned directory should be a symlink
            string versionedSymlink = Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0", "link.dll");
            IsSymlink(versionedSymlink).Should().BeTrue("versioned symlink should be preserved as a symbolic link");

            // The symlink should resolve to the original file
            string target = File.ReadAllText(versionedSymlink);
            string originalContent = File.ReadAllText(originalFile);
            target.Should().Be(originalContent, "symlink should resolve to the same content as the original");
        }

        /// <summary>
        /// Verifies that symbolic links in non-versioned locations (root level)
        /// are preserved after extraction.
        /// </summary>
        [Fact]
        public void SymlinksInNonVersionedLocationsArePreserved()
        {
            SkipUnlessPrerequisitesMet();

            string tarPath = CreateTarballWithSymlinks();
            string outPath = Path.Combine(_testDir, "out");
            Directory.CreateDirectory(outPath);

            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);
            result.ExitCode.Should().Be(0, $"extraction should succeed.\nStderr: {result.StdErr}");

            // The root-level symlink should be a symlink
            string rootSymlink = Path.Combine(outPath, "dotnet-link");
            IsSymlink(rootSymlink).Should().BeTrue("root-level symlink should be preserved as a symbolic link");
        }

        /// <summary>
        /// Verifies that a versioned directory containing only symlinks (no regular files)
        /// is still extracted. This directly tests that find includes -type l.
        /// </summary>
        [Fact]
        public void VersionedDirectoryWithOnlySymlinksIsExtracted()
        {
            SkipUnlessPrerequisitesMet();

            string tarPath = CreateTarballWithSymlinkOnlyVersionedDir();
            string outPath = Path.Combine(_testDir, "out");
            Directory.CreateDirectory(outPath);

            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);
            _output.WriteLine($"Exit code: {result.ExitCode}");
            _output.WriteLine($"Stderr: {result.StdErr}");
            result.ExitCode.Should().Be(0, $"extraction should succeed.\nStderr: {result.StdErr}");

            // The symlink-only versioned directory should exist
            string symlinkOnlyDir = Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0");
            Directory.Exists(symlinkOnlyDir).Should().BeTrue("versioned directory with only symlinks should be extracted");

            string symlink = Path.Combine(symlinkOnlyDir, "link-to-host.dll");
            IsSymlink(symlink).Should().BeTrue("symlink in symlink-only versioned dir should be preserved");
        }

        /// <summary>
        /// Verifies that cross-directory symbolic links are preserved after extraction.
        /// </summary>
        [Fact]
        public void CrossDirectorySymlinksArePreserved()
        {
            SkipUnlessPrerequisitesMet();

            string tarPath = CreateTarballWithSymlinks();
            string outPath = Path.Combine(_testDir, "out");
            Directory.CreateDirectory(outPath);

            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);
            result.ExitCode.Should().Be(0, $"extraction should succeed.\nStderr: {result.StdErr}");

            // Cross-directory symlink from sdk/ pointing to shared/
            string crossDirSymlink = Path.Combine(outPath, "sdk", "11.0.100", "linked-from-shared.dll");
            IsSymlink(crossDirSymlink).Should().BeTrue("cross-directory symlink should be preserved");

            // It should point to a relative path
            var linkTarget = new FileInfo(crossDirSymlink).LinkTarget;
            linkTarget.Should().NotBeNull("symlink should have a target");
            linkTarget.Should().NotStartWith("/", "symlink should be relative, not absolute");
        }

        private string CreateTarballWithSymlinks()
        {
            var tarPath = Path.Combine(_testDir, "test.tar.gz");
            using var fileStream = File.Create(tarPath);
            using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);
            using var tarWriter = new TarWriter(gzipStream);

            // Real files
            WriteFileEntry(tarWriter, "shared/Microsoft.NETCore.App/11.0.0/original.dll", "original-content");
            WriteFileEntry(tarWriter, "dotnet", "dotnet-host-binary");
            WriteFileEntry(tarWriter, "sdk/11.0.100/sdk-file.dll", "sdk-content");

            // Symlink within versioned directory
            WriteSymlinkEntry(tarWriter, "shared/Microsoft.NETCore.App/11.0.0/link.dll", "original.dll");
            // Root-level symlink (non-versioned)
            WriteSymlinkEntry(tarWriter, "dotnet-link", "dotnet");
            // Cross-directory symlink
            WriteSymlinkEntry(tarWriter, "sdk/11.0.100/linked-from-shared.dll", "../../shared/Microsoft.NETCore.App/11.0.0/original.dll");

            return tarPath;
        }

        private string CreateTarballWithSymlinkOnlyVersionedDir()
        {
            // A versioned directory that contains ONLY symlinks — no regular files.
            // This is the key regression test: if find only uses -type f, this directory
            // will not appear in the versioned directory list and won't be copied at all.
            var tarPath = Path.Combine(_testDir, "symonly.tar.gz");
            using var fileStream = File.Create(tarPath);
            using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);
            using var tarWriter = new TarWriter(gzipStream);

            // Real file in host/fxr (a different versioned dir)
            WriteFileEntry(tarWriter, "host/fxr/11.0.0/hostfxr.dll", "hostfxr-content");
            // dotnet binary at root
            WriteFileEntry(tarWriter, "dotnet", "dotnet-host-binary");
            // Versioned directory with ONLY a symlink (no regular files)
            WriteSymlinkEntry(tarWriter, "shared/Microsoft.NETCore.App/11.0.0/link-to-host.dll", "../../../host/fxr/11.0.0/hostfxr.dll");

            return tarPath;
        }

        private static void WriteFileEntry(TarWriter writer, string path, string content)
        {
            var entry = new PaxTarEntry(TarEntryType.RegularFile, path)
            {
                DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
            };
            writer.WriteEntry(entry);
        }

        private static void WriteSymlinkEntry(TarWriter writer, string path, string target)
        {
            var entry = new PaxTarEntry(TarEntryType.SymbolicLink, path)
            {
                LinkName = target
            };
            writer.WriteEntry(entry);
        }

        private ExtractionResult RunBash(string script)
        {
            string scriptPath = Path.Combine(_testDir, "run-" + Path.GetRandomFileName() + ".sh");
            File.WriteAllText(scriptPath, script);

            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = scriptPath,
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

        private static bool IsSymlink(string path)
        {
            var info = new FileInfo(path);
            return info.Exists && info.LinkTarget != null;
        }

        private static string GetScriptPath()
        {
            string? directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, ".git")))
            {
                directory = Directory.GetParent(directory)?.FullName;
            }
            return Path.Combine(directory ?? ".", "src", "dotnet-install.sh");
        }
    }
}
