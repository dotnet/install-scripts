// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.DataService.Kusto;
using Newtonsoft.Json;
using System;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Represents a script execution event to be inserted into Kusto.
    /// </summary>
    internal class ScriptExecutionLogEntry : IKustoTableRow
    {
        [JsonProperty("monitor_name"), JsonRequired]
        public string? MonitorName { get; set; }

        [JsonProperty("timestamp"), JsonRequired]
        public DateTime EventTime { get; set; }

        [JsonProperty("script_name")]
        public string? ScriptName { get; set; }

        [JsonProperty("cmd_args")]
        public string? CommandLineArgs { get; set; }

        [JsonProperty("error")]
        public string? Error { get; set; }
    }
}
