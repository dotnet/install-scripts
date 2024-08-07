#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Install_Scripts.Test.Utils
{
    /// <summary>
    /// This command is designed to automate the installation and invocation of .NET Core SDK.
    /// </summary>
    internal sealed class DotNetCommand(IEnumerable<string> args)
    {
        private const string ScriptName = "dotnet-install";

        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private readonly IEnumerable<string> _scriptArgs = args;

        internal CommandResult ExecuteInstallation() => RunProcess(SetupScriptsExecutionArgs());

        internal CommandResult ExecuteDotnetCommand(string dotnetPath) => RunProcess(SetupDotnetExecutionArgs(dotnetPath));

        private CommandResult RunProcess(string processArgs)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = GetProcessName(),
                Arguments = processArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();

            string errors = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new CommandResult(startInfo, process.ExitCode, output, errors);
        }

        private string GetProcessName() => IsWindows ? "powershell.exe" : @"/bin/bash";

        private string GetDotnetExecutablePath(string? dotnetPath) => string.IsNullOrEmpty(dotnetPath) ? string.Empty : $"{Path.Combine(dotnetPath!, "dotnet")}";

        /// <summary>
        /// Sets up the args required for executing the .NET Core installation script.
        /// </summary>
        /// <returns>The args required for script execution.</returns>
        private string SetupScriptsExecutionArgs()
        {
            string scriptExtension = IsWindows ? "ps1" : "sh";
            string scriptPath = Path.Combine(Path.Combine(GetRepoRoot() ?? string.Empty, "src", $"{ScriptName}.{scriptExtension}"));
            
            return IsWindows
                    ? $"-ExecutionPolicy Bypass -NoProfile -NoLogo -Command \" {scriptPath} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_scriptArgs)}"
                    : $"{scriptPath} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_scriptArgs)}";
        }

        /// <summary>
        /// Sets args required for invocation for the installed dotnet.
        /// </summary>
        /// <returns>The args required for dotnet invocation.</returns>
        private string SetupDotnetExecutionArgs(string dotnetPath) => IsWindows
                    ? $" {GetDotnetExecutablePath(dotnetPath)} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_scriptArgs)}"
                    : $"-c \"{GetDotnetExecutablePath(dotnetPath)} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_scriptArgs)}\"";

        private static string? GetRepoRoot()
        {
            string? directory = AppContext.BaseDirectory;

            while (directory!= null && !Directory.Exists(Path.Combine(directory, ".git")) && directory != null)
            {
                directory = Directory.GetParent(directory)?.FullName;
            }

            if (directory == null)
            {
                return null;
            }

            return directory;
        }

        internal readonly struct CommandResult
        {
            internal CommandResult(ProcessStartInfo startInfo, int exitCode, string? stdOut, string? stdErr)
            {
                StartInfo = startInfo;
                ExitCode = exitCode;
                StdOut = stdOut;
                StdErr = stdErr;
            }

            internal ProcessStartInfo StartInfo { get; }

            internal int ExitCode { get; }

            internal string? StdOut { get; }

            internal string? StdErr { get; }
        }
    }
}
