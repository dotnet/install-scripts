using Install_Scripts.Test.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    internal static class TestUtils
    {
        internal static DotNetCommand CreateDotnetCommand(IEnumerable<string> args) => new DotNetCommand(args);

        internal static DotNetCommand CreateInstallCommand(IEnumerable<string> args) => new DotNetCommand(args);

        internal static DotNetCommand CreateInstallCommand(IEnumerable<string> args, IDictionary<string, string> environmentVariables) => new DotNetCommand(args, environmentVariables);

        private const int DeleteAttempts = 8;
        private static readonly TimeSpan InitialDeleteRetryDelay = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan MaxDeleteRetryDelay = TimeSpan.FromSeconds(1);

        /// <remarks>
        /// Windows keeps an executable locked while it runs and sometimes briefly after it exits
        /// (for example while antivirus scans it), so deleting right after running dotnet can fail.
        /// </remarks>
        internal static void DeleteInstallationDirectory(string path)
        {
            TimeSpan delay = InitialDeleteRetryDelay;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    Directory.Delete(path, true);
                    return;
                }
                catch (DirectoryNotFoundException)
                {
                    // Directory to cleanup may not be there if installation fails. Not an issue. Ignore the exception.
                    return;
                }
                catch (Exception e) when (e is UnauthorizedAccessException or IOException)
                {
                    if (attempt == DeleteAttempts)
                    {
                        throw new Exception($"Failed to remove {path}", e);
                    }

                    Thread.Sleep(delay);
                    delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxDeleteRetryDelay.Ticks));
                }
            }
        }
    }
}
