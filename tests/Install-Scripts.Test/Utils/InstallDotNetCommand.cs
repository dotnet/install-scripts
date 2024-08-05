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
    /// This command is designed to automate the installation of .NET Core SDK.
    /// </summary>
    public sealed class InstallDotNetCommand
    {
        private const string ScriptName = "dotnet-install";

        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private IEnumerable<string> _args = Enumerable.Empty<string>();

        private string? _processName;

        public InstallDotNetCommand(IEnumerable<string> args, string? processName = null)
        {
            _args = args;
            _processName = processName;
        }

        public CommandResult ExecuteCommand()
        {
            ScriptExecutionSettings executionSettings = SetupScriptsExecutionSettings();

            return RunScript(executionSettings);
        }

        /// <summary>
        /// Runs the .NET Core installation script with the specified settings.
        /// </summary>
        /// <param name="executionSettings">The settings required for script execution.</param>
        /// <returns>True if the script executed successfully; otherwise, false.</returns>
        private CommandResult RunScript(ScriptExecutionSettings executionSettings)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = string.IsNullOrEmpty(_processName) ? GetProcessName() : _processName,
                Arguments = executionSettings.ExecutableArgs,
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

        private static string GetProcessName() => IsWindows ? "powershell.exe" : @"/bin/bash";

        /// <summary>
        /// Sets up the settings required for executing the .NET Core installation script.
        /// </summary>
        /// <returns>The settings required for script execution.</returns>
        private ScriptExecutionSettings SetupScriptsExecutionSettings()
        {
            string scriptExtension = IsWindows ? "ps1" : "sh";
            string scriptPath = Path.Combine(Path.Combine(GetRepoRoot() ?? string.Empty, "src", $"{ScriptName}.{scriptExtension}"));
            string scriptArgs = IsWindows
                ? $"-ExecutionPolicy Bypass -NoProfile -NoLogo -Command \" {scriptPath} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_args)} + \""
                : $"{scriptPath} {ArgumentEscaper.EscapeAndConcatenateArgArrayForProcessStart(_args)}";

            return new ScriptExecutionSettings($"{ScriptName}.{scriptExtension}", scriptPath, scriptArgs);
        }

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

        /// <summary>
        /// A private struct to hold settings for script execution.
        /// </summary>
        private readonly struct ScriptExecutionSettings(string scriptName, string scriptsFullPath, string executableArgs)
        {
            public string ScriptName { get; } = scriptName;

            public string ScriptsFullPath { get; } = scriptsFullPath;

            public string ExecutableArgs { get; } = executableArgs;
        }

        public readonly struct CommandResult
        {
            internal static readonly CommandResult Empty;

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
