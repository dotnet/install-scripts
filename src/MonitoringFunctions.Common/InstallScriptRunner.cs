// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.Models;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MonitoringFunctions
{
    /// <summary>
    /// This type contains the methods to run install scripts with specified parameters and parse the results.
    /// </summary>
    internal static class InstallScriptRunner
    {
        /// <summary>
        /// Executes the install script with given arguments.
        /// </summary>
        /// <param name="commandLineArgs">Arguments to be passed to the script.</param>
        /// <returns>Returns a <see cref="ScriptExecutionResult"/> object containing the output of the execution.</returns>
        /// <remarks>The script to be run depends on the platform. On windows, powershell script is run.
        /// On other operating systems, bash script is used.</remarks>
        public static async Task<ScriptExecutionResult> ExecuteInstallScriptAsync(string? commandLineArgs)
        {
            ProcessStartInfo processStartInfo = GetScriptProcessStartInfo(out string scriptName, commandLineArgs);

            processStartInfo.CreateNoWindow = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;

            Process installScriptProc = Process.Start(processStartInfo);
            string consoleOutput = await installScriptProc.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            string consoleError = await installScriptProc.StandardError.ReadToEndAsync().ConfigureAwait(false);

            return new ScriptExecutionResult()
            {
                ScriptName = scriptName,
                Output = consoleOutput,
                Error = consoleError
            };
        }

        /// <summary>
        /// Parses the output of the script when executed in DryRun mode and finds out Primary and Legacy urls.
        /// </summary>
        /// <param name="output">Output of the script execution in DryRun mode</param>
        /// <returns>Object containing primary and legacy runs</returns>
        public static ScriptDryRunResult ParseDryRunOutput(string? output)
        {
            string primaryUrlIdentifier = "Primary named payload URL: ";
            string legacyUrlIdentifier = "Legacy named payload URL: ";

            ScriptDryRunResult result = new ScriptDryRunResult();

            if (output == null)
            {
                return result;
            }

            using StringReader streamReader = new StringReader(output);

            string? line;
            while ((line = streamReader.ReadLine()) != null)
            {
                // Does this line contain the primary url?
                int primaryIdIndex = line.IndexOf(primaryUrlIdentifier);
                if (primaryIdIndex != -1)
                {
                    result.PrimaryUrl = line.Substring(primaryIdIndex + primaryUrlIdentifier.Length);
                }
                else
                {
                    // Does this line contain the legacy url?
                    int legacyIdIndex = line.IndexOf(legacyUrlIdentifier);
                    if (legacyIdIndex != -1)
                    {
                        result.LegacyUrl = line.Substring(legacyIdIndex + legacyUrlIdentifier.Length);
                    }
                }
            }

            return result;
        }

        private static ProcessStartInfo GetScriptProcessStartInfo(out string scriptName, string? args = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                scriptName = "dotnet-install.ps1";
                return new ProcessStartInfo("powershell",
                    @"-NoProfile -Command ""[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;" +
                    @" $ProgressPreference = 'SilentlyContinue'; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing" +
                    @" 'https://dot.net/v1/dotnet-install.ps1')))" +
                    $@" {args}""");
            }

            scriptName = "dotnet-install.sh";
            return new ProcessStartInfo("bash",
                $@"-c ""curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin {args?.Replace("\"", "\\\"")}""");
        }
    }
}
