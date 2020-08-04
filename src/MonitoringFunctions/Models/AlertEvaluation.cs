// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Stores the details of evaluation that resulted with an alert triggering
    /// </summary>
    internal struct AlertEvaluation : IEquatable<AlertEvaluation>
    {
        [JsonProperty("value")]
        public float Value { get; set; }

        [JsonProperty("metric")]
        public string? Metric { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, string>? Tags { get; set; }

        public bool Equals(AlertEvaluation other)
        {
            bool equals = Value == other.Value && Metric == other.Metric;

            if (!equals)
            {
                return false;
            }

            if (Tags == other.Tags)
            {
                return true;
            }

            if (Tags == null || other.Tags == null)
            {
                return false;
            }

            return Tags.Count == other.Tags.Count && !Tags.Except(other.Tags).Any();
        }

        public override string ToString()
        {
            return $"{Metric}: {Value}";
        }
    }
}
