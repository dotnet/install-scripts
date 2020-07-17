// Copyright (c) Microsoft. All rights reserved.

namespace MonitoringFunctions.Models
{
    /// <summary> Stores the contents of output streams of a script execution. </summary>
    internal class ScriptExecutionResult
    {
        /// <summary> Contents of the output stream as string </summary>
        public string? Output { get; set; }

        /// <summary> Contents of the error stream as string </summary>
        public string? Error { get; set; }
    }
}
