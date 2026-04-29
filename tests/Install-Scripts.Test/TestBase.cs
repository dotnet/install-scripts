using System;
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
            : base(settings, ResolveAssetsSourceFile(sourceFile)) { }

        private static string ResolveAssetsSourceFile(string sourceFile)
        {
            string sourceDir = Path.GetDirectoryName(sourceFile) ?? "";
            string sourceAssetsDir = Path.Combine(sourceDir, "Assets");

            // On Helix, the compile-time source path does not exist.
            // Fall back to the output directory where Assets are copied as Content items.
            if (!Directory.Exists(sourceAssetsDir))
            {
                return Path.Combine(AppContext.BaseDirectory, "Assets", "foo.cs");
            }

            return Path.Combine(sourceAssetsDir, "foo.cs");
        }
    }
}
