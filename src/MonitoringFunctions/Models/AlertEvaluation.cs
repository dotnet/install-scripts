// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Stores the details of evaluation that resulted with an alert triggering
    /// </summary>
    internal sealed class AlertEvaluation
    {
        [JsonProperty("value")]
        public float Value { get; set; }

        [JsonProperty("metric")]
        public string? Metric { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, string>? Tags { get; set; }
    }
}
