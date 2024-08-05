using Install_Scripts.Test.Utils;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    internal static class TestUtils
    {
        internal static InstallDotNetCommand CreateDotnetCommand(string workingDirectory, IEnumerable<string> args) => new InstallDotNetCommand(args, Path.Combine(workingDirectory, "dotnet"));

        internal static InstallDotNetCommand CreateInstallCommand(IEnumerable<string> args) => new InstallDotNetCommand(args);
    }
}
