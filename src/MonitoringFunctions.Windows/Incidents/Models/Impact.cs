// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// Describes an incident's individual impact.
    /// </summary>
    [DataContract]
    internal struct Impact : IEquatable<Impact>
    {
        /// <summary>
        /// <see cref="string"/> describing the impact.
        /// </summary>
        [DataMember(Name = "impact-description")]
        public string? Description { get; set; }

        /// <summary>
        /// <see cref="ImpactedScript"/> detailing the impacted scripts.
        /// </summary>
        [DataMember(Name = "impacted-script")]
        public ImpactedScript Script { get; set; }


        public override string ToString()
        {
            return $"Impact: {Description ?? string.Empty} ({Script})";
        }

        public override bool Equals(object? obj)
        {
            return obj is Impact other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Description?.GetHashCode() ?? 0) ^ Script.GetHashCode();
        }

        public bool Equals(Impact other)
        {
            return other.Description == Description && other.Script == Script;
        }
    }
}