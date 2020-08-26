// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.DataService.Kusto;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Represents a script execution event to be inserted into Kusto.
    /// </summary>
    internal struct ScriptExecutionLogEntry : IEquatable<ScriptExecutionLogEntry>, IKustoTableRow
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

        public bool Equals([AllowNull] ScriptExecutionLogEntry other)
        {
            return MonitorName == other.MonitorName &&
                EventTime == other.EventTime &&
                ScriptName == other.ScriptName &&
                CommandLineArgs == other.CommandLineArgs &&
                Error == Error;
        }

        public override string? ToString()
        {
            return $"ScriptExecutionLogEntry - MonitorName: {MonitorName}, EventTime: {EventTime}, ScriptName: {ScriptName}, CommandLineArgs: {CommandLineArgs}, Error: {Error}";
        }
    }
}
