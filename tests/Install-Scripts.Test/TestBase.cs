using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Microsoft.DotNet.InstallationScript.Tests
{
    public abstract class TestBase
    {
        private const string TraceLogMarker = "#ISCO-TRACE";
        private const string ErrorLogMarker = "#ISCO-ERROR";
        private const string LogMarkerDelimiter = "!!";

        private readonly ITestOutputHelper outputHelper;

        public TestBase(ITestOutputHelper testOutputHelper)
        {
            outputHelper = testOutputHelper;
        }

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

        // Transfers information to custom test adapter. E.g. install-scripts-monitoring -> TestLogger
        protected void PopulateTestLoggerOutput(CommandResult commandResult)
        {
            if(!string.IsNullOrEmpty(commandResult.StdOut))
            {
                outputHelper.WriteLine(GetFormattedLogOutput(TraceLogMarker, commandResult.StdOut));
            }

            if(!string.IsNullOrEmpty(commandResult.StdErr))
            {
                outputHelper.WriteLine(GetFormattedLogOutput(ErrorLogMarker, commandResult.StdErr));
            }
        }

        private string GetFormattedLogOutput(string logMarker, string message) =>
            $"{logMarker}-START{LogMarkerDelimiter}:{message}{logMarker}-END-{LogMarkerDelimiter}";
    }
}
