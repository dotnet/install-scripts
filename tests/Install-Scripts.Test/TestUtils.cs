using Install_Scripts.Test.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    internal static class TestUtils
    {
        internal static DotNetCommand CreateDotnetCommand(IEnumerable<string> args) => new DotNetCommand(args);

        internal static DotNetCommand CreateInstallCommand(IEnumerable<string> args) => new DotNetCommand(args);

        internal static DotNetCommand CreateInstallCommand(IEnumerable<string> args, IDictionary<string, string> environmentVariables) => new DotNetCommand(args, environmentVariables);

        internal static void DeleteInstallationDirectory(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (DirectoryNotFoundException)
            {
                // Directory to cleanup may not be there if installation fails. Not an issue. Ignore the exception.
            }
            catch (UnauthorizedAccessException e)
            {
                throw new Exception($"Failed to remove {path}", e);
            }
        }
    }
}
