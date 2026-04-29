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

        internal CommandResult ExecuteInstallation()
        {
            if (!IsWindows)
            {
                string scriptPath = GetInstallScriptPath();
                if (File.Exists(scriptPath))
                {
                    // Ensure the script is executable (e.g. on Helix where the
                    // correlation payload may not preserve Unix permissions).
                    Process.Start("chmod", $"+x {scriptPath}")?.WaitForExit();
                }
            }

            return RunProcess(SetupScriptsExecutionArgs());
        }

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

        // pwsh (PowerShell Core 7+) is used instead of powershell.exe (Windows PowerShell 5.x) because
        // it has significantly faster startup time, reducing overhead across the many test invocations.
        // PowerShell Core is pre-installed on all modern Azure Pipelines Windows agents and is also
        // the recommended shell for cross-platform automation.
        private string GetProcessName() => IsWindows ? "pwsh" : @"/bin/bash";

        private string GetDotnetExecutablePath(string? dotnetPath) => string.IsNullOrEmpty(dotnetPath) ? string.Empty : $"{Path.Combine(dotnetPath!, "dotnet")}";

        /// <summary>
        /// Sets up the args required for executing the .NET Core installation script.
        /// </summary>
        /// <returns>The args required for script execution.</returns>
        private static string GetInstallScriptPath()
        {
            string scriptExtension = IsWindows ? "ps1" : "sh";
            return Path.Combine(GetRepoRoot() ?? string.Empty, "src", $"{ScriptName}.{scriptExtension}");
        }

        private string SetupScriptsExecutionArgs()
        {
            string scriptPath = GetInstallScriptPath();
            
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
            // On Helix, the repo source is provided via the correlation payload
            string? helixPayload = Environment.GetEnvironmentVariable("HELIX_CORRELATION_PAYLOAD");
            if (!string.IsNullOrEmpty(helixPayload) && Directory.Exists(Path.Combine(helixPayload, "src")))
            {
                return helixPayload;
            }

            string? directory = AppContext.BaseDirectory;

            while (directory != null && !Directory.Exists(Path.Combine(directory, ".git")))
            {
                directory = Directory.GetParent(directory)?.FullName;
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
