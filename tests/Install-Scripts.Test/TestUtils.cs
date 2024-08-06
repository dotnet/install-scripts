using Install_Scripts.Test.Utils;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    internal static class TestUtils
    {
        internal static DotNetCommand CreateDotnetCommand(IEnumerable<string> args) => new DotNetCommand(args);

        internal static DotNetCommand CreateInstallCommand(IEnumerable<string> args) => new DotNetCommand(args);
    }
}
