// Copyright (c) Microsoft. All rights reserved.

namespace MonitoringFunctions.Models
{
    /// <summary> Stores the contents of output streams of a script execution. </summary>
    internal sealed class ScriptExecutionResult
    {
        /// <summary> Contents of the output stream as string </summary>
        public string? Output { get; set; }

        /// <summary> Contents of the error stream as string </summary>
        public string? Error { get; set; }

        public override string? ToString()
        {
            return $"ScriptExecutionResult - Output: {Output}, Error: {Error}";
        }
    }
}
