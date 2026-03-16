using System.IO;
using System.Runtime.CompilerServices;
using VerifyTests;
using VerifyXunit;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public abstract class TestBase : VerifyBase
    {
        // It's needed to resolve the path to test assets for verification.
        protected TestBase(VerifySettings? settings = null, [CallerFilePath] string sourceFile = "")
            : base(settings, Path.Combine(Path.GetDirectoryName(sourceFile) ?? "", "Assets", "foo.cs")) { }
    }
}
