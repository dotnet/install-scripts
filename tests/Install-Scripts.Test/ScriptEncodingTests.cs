// Copyright (c) Microsoft. All rights reserved.

using System;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    /// <summary>
    /// Guards against non-ASCII characters creeping into the install scripts.
    ///
    /// When a signed PowerShell script contains non-ASCII characters and is saved
    /// without a byte-order mark, its Authenticode content hash fails validation on
    /// machines whose active code page differs from the signing machine, producing:
    /// "the hash of the file does not match the hash stored in the digital signature".
    /// This has happened twice (issues #541 and #729), so this test fails the build
    /// the moment any non-ASCII byte is introduced into either script.
    /// See https://learn.microsoft.com/troubleshoot/windows-client/system-management-components/signed-powershell-script-fails-hash-mismatch
    /// </summary>
    public class ScriptEncodingTests
    {
        [Theory]
        [InlineData("dotnet-install.ps1")]
        [InlineData("dotnet-install.sh")]
        public void Script_contains_only_ASCII_characters(string scriptFileName)
        {
            string scriptPath = Path.Combine(GetRepoRoot(), "src", scriptFileName);
            Assert.True(File.Exists(scriptPath), $"Could not locate script at '{scriptPath}'.");

            byte[] bytes = File.ReadAllBytes(scriptPath);

            int line = 1;
            for (int i = 0; i < bytes.Length; i++)
            {
                byte b = bytes[i];
                if (b == (byte)'\n')
                {
                    line++;
                    continue;
                }

                if (b > 127)
                {
                    string message =
                        $"Non-ASCII byte 0x{b:X2} found in '{scriptFileName}' at line {line} (byte offset {i}). " +
                        "Signed PowerShell scripts must contain only ASCII characters to avoid hash-mismatch " +
                        "failures across locales (issues #541, #729). Replace it with an ASCII equivalent.";
                    Assert.Fail(message);
                }
            }
        }

        private static string GetRepoRoot()
        {
            string? directory = AppContext.BaseDirectory;

            // The repo root is the directory containing ".git". In a normal clone that is a
            // directory; in a git worktree it is a file (a gitdir pointer), so check for both.
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory, ".git")) &&
                   !File.Exists(Path.Combine(directory, ".git")))
            {
                directory = Directory.GetParent(directory)?.FullName;
            }

            Assert.True(directory != null, "Could not locate the repository root (no '.git' found).");
            return directory!;
        }
    }
}
