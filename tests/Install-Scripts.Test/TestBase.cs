using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using System.Collections.Generic;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public abstract class TestBase
    {
        protected static Command CreateInstallCommand(IEnumerable<string> args)
        {
            string path;
            string finalArgs;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                path = "powershell.exe";
                finalArgs = "-ExecutionPolicy Bypass -NoProfile -NoLogo -Command \"" +
                    Path.Combine(GetRepoRoot(), "src", "dotnet-install.ps1") + " " + ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args) + "\"";
            }
            else
            {
                path = Path.Combine(GetRepoRoot(), "src", "dotnet-install.sh");
                finalArgs = ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(args);
            }

            return Command.Create(new CommandSpec(path, finalArgs, CommandResolutionStrategy.None));
        }

        protected static string GetRepoRoot()
        {
            string directory = AppContext.BaseDirectory;

            while (!Directory.Exists(Path.Combine(directory, ".git")) && directory != null)
            {
                directory = Directory.GetParent(directory)?.FullName;
            }

            if (directory == null)
            {
                return null;
            }
            return directory;
        }
    }
}
