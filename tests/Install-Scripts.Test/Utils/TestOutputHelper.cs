using Microsoft.DotNet.Cli.Utils;
using Xunit.Abstractions;

namespace Install_Scripts.Test.Utils
{
    internal class TestOutputHelper
    {
        private const string TraceLogMarker = "#ISCO-TRACE";
        private const string ErrorLogMarker = "#ISCO-ERROR";
        private const string LogMarkerDelimiter = "!!";

        // Transfers information to custom test adapter. E.g. install-scripts-monitoring -> TestLogger
        public static void PopulateTestLoggerOutput(ITestOutputHelper outputHelper, CommandResult commandResult)
        {
            if (!string.IsNullOrEmpty(commandResult.StdOut))
            {
                outputHelper.WriteLine(GetFormattedLogOutput(TraceLogMarker, commandResult.StdOut));
            }

            if (!string.IsNullOrEmpty(commandResult.StdErr))
            {
                outputHelper.WriteLine(GetFormattedLogOutput(ErrorLogMarker, commandResult.StdErr));
            }
        }

        private static string GetFormattedLogOutput(string logMarker, string message) =>
            $"{logMarker}-START{LogMarkerDelimiter}:{message}{logMarker}-END-{LogMarkerDelimiter}";
    }
}
