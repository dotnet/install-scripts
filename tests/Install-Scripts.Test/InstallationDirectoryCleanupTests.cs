using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public class InstallationDirectoryCleanupTests
    {
        /// <summary>
        /// Windows denies deleting an executable while it is still running, and can keep it locked
        /// briefly after it exits. Cleanup right after <c>dotnet --info</c> must survive that.
        /// </summary>
        [Fact]
        public void WhenAnExecutableInTheDirectoryIsStillRunningCleanupWaitsForIt()
        {
            Assert.SkipUnless(RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "Only Windows refuses to delete a running executable.");

            string directory = Path.Combine(Path.GetTempPath(), "InstallScript-Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            string dotnetPath = Path.Combine(directory, "dotnet.exe");
            File.Copy(Path.Combine(Environment.SystemDirectory, "ping.exe"), dotnetPath);

            // ping -n 2 runs for about a second, so the executable is locked when cleanup starts.
            using var process = Process.Start(new ProcessStartInfo(dotnetPath, "-n 2 127.0.0.1")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
            })!;

            TestUtils.DeleteInstallationDirectory(directory);

            Assert.False(Directory.Exists(directory));
        }
    }
}
