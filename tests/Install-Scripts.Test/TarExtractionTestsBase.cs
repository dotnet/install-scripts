// Copyright (c) Microsoft. All rights reserved.

using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Base class for tar extraction tests that exercise the install script's extraction logic.
    /// Shared test scenarios verify versioned-directory skip logic, non-versioned file override
    /// behavior, and basic extraction correctness. Derived classes provide platform-specific
    /// execution (PowerShell on Windows, bash on Linux).
    /// </summary>
    public abstract class TarExtractionTestsBase : IDisposable
    {
        protected readonly string _testDir;
        protected readonly ITestOutputHelper _output;

        protected TarExtractionTestsBase(ITestOutputHelper output)
        {
            _output = output;
            _testDir = Path.Combine(Path.GetTempPath(), GetType().Name, Path.GetRandomFileName());
            Directory.CreateDirectory(_testDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testDir))
                {
                    Directory.Delete(_testDir, recursive: true);
                }
            }
            catch (DirectoryNotFoundException) { }
            catch (UnauthorizedAccessException) { }
        }

        /// <summary>
        /// Runs the platform-specific extraction function against the given tarball.
        /// </summary>
        protected abstract ExtractionResult RunExtraction(string tarPath, string outPath, bool overrideNonVersionedFiles);

        /// <summary>
        /// Checks that prerequisites are met for this platform; skips the test if not.
        /// </summary>
        protected abstract void SkipUnlessPrerequisitesMet();

        /// <summary>
        /// Verifies that a basic tarball is fully extracted to the output directory.
        /// </summary>
        [Fact]
        public void BasicTarballIsExtractedCorrectly()
        {
            SkipUnlessPrerequisitesMet();

            var tarPath = CreateTarball("basic.tar.gz", new Dictionary<string, string>
            {
                ["dotnet"] = "dotnet-binary",
                ["shared/Microsoft.NETCore.App/11.0.0/System.Runtime.dll"] = "runtime-dll",
                ["sdk/11.0.100/dotnet.dll"] = "sdk-dll"
            });

            var outPath = Path.Combine(_testDir, "install-basic");
            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);

            result.ExitCode.Should().Be(0, because: $"extraction should succeed.\nStdOut: {result.StdOut}\nStdErr: {result.StdErr}");
            File.Exists(Path.Combine(outPath, "dotnet")).Should().BeTrue("non-versioned file should be extracted");
            File.Exists(Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0", "System.Runtime.dll")).Should().BeTrue("versioned file should be extracted");
            File.Exists(Path.Combine(outPath, "sdk", "11.0.100", "dotnet.dll")).Should().BeTrue("SDK file should be extracted");
        }

        /// <summary>
        /// Verifies that already-installed versioned directories are skipped during extraction.
        /// </summary>
        [Fact]
        public void ExistingVersionedDirectoryIsSkipped()
        {
            SkipUnlessPrerequisitesMet();

            var tarPath = CreateTarball("versioned.tar.gz", new Dictionary<string, string>
            {
                ["shared/Microsoft.NETCore.App/11.0.0/System.Runtime.dll"] = "new-content",
                ["shared/Microsoft.NETCore.App/11.0.1/System.Runtime.dll"] = "newer-content"
            });

            var outPath = Path.Combine(_testDir, "install-versioned");
            // Pre-create the 11.0.0 directory with existing content
            var existingDir = Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0");
            Directory.CreateDirectory(existingDir);
            File.WriteAllText(Path.Combine(existingDir, "System.Runtime.dll"), "old-content");

            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);

            result.ExitCode.Should().Be(0, because: $"extraction should succeed.\nStdOut: {result.StdOut}\nStdErr: {result.StdErr}");
            // 11.0.0 should be untouched (already existed)
            File.ReadAllText(Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.0", "System.Runtime.dll"))
                .Should().Be("old-content", "existing versioned directory should not be overwritten");
            // 11.0.1 should be extracted (new version)
            File.ReadAllText(Path.Combine(outPath, "shared", "Microsoft.NETCore.App", "11.0.1", "System.Runtime.dll"))
                .Should().Be("newer-content", "new versioned directory should be extracted");
        }

        /// <summary>
        /// Verifies that non-versioned files are not overwritten when OverrideNonVersionedFiles is false.
        /// </summary>
        [Fact]
        public void NonVersionedFilesAreNotOverwrittenWhenSkipNonVersionedFilesIsSet()
        {
            SkipUnlessPrerequisitesMet();

            var tarPath = CreateTarball("nonversioned.tar.gz", new Dictionary<string, string>
            {
                ["dotnet"] = "new-binary",
                ["LICENSE.txt"] = "new-license"
            });

            var outPath = Path.Combine(_testDir, "install-nonversioned");
            Directory.CreateDirectory(outPath);
            File.WriteAllText(Path.Combine(outPath, "dotnet"), "old-binary");

            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: false);

            result.ExitCode.Should().Be(0, because: $"extraction should succeed.\nStdOut: {result.StdOut}\nStdErr: {result.StdErr}");
            File.ReadAllText(Path.Combine(outPath, "dotnet"))
                .Should().Be("old-binary", "existing non-versioned file should not be overwritten");
            File.ReadAllText(Path.Combine(outPath, "LICENSE.txt"))
                .Should().Be("new-license", "new non-versioned file should be extracted");
        }

        /// <summary>
        /// Verifies that non-versioned files ARE overwritten when OverrideNonVersionedFiles is true.
        /// </summary>
        [Fact]
        public void NonVersionedFilesAreOverwrittenWhenOverrideIsSet()
        {
            SkipUnlessPrerequisitesMet();

            var tarPath = CreateTarball("override.tar.gz", new Dictionary<string, string>
            {
                ["dotnet"] = "new-binary"
            });

            var outPath = Path.Combine(_testDir, "install-override");
            Directory.CreateDirectory(outPath);
            File.WriteAllText(Path.Combine(outPath, "dotnet"), "old-binary");

            var result = RunExtraction(tarPath, outPath, overrideNonVersionedFiles: true);

            result.ExitCode.Should().Be(0, because: $"extraction should succeed.\nStdOut: {result.StdOut}\nStdErr: {result.StdErr}");
            File.ReadAllText(Path.Combine(outPath, "dotnet"))
                .Should().Be("new-binary", "non-versioned file should be overwritten when override is set");
        }

        /// <summary>
        /// Creates a tarball with the specified file entries using .NET's TarWriter API.
        /// </summary>
        protected string CreateTarball(string name, Dictionary<string, string> files)
        {
            var tarPath = Path.Combine(_testDir, name);
            using var fileStream = File.Create(tarPath);
            using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);
            using var tarWriter = new TarWriter(gzipStream);

            foreach (var (entryPath, content) in files)
            {
                var entry = new PaxTarEntry(TarEntryType.RegularFile, entryPath)
                {
                    DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content))
                };
                tarWriter.WriteEntry(entry);
            }

            return tarPath;
        }

        protected record ExtractionResult(int ExitCode, string StdOut, string StdErr);
    }
}
