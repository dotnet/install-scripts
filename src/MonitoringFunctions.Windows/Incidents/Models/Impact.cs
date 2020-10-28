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
        /// <see cref="ImpactedScript"/> detailing the impacted scripts.
        /// </summary>
        [DataMember(Name = "impacted-script")]
        public ImpactedScript Script { get; set; }

        /// <summary>
        /// <see cref="CommonContent"/> describing impact.
        /// </summary>
        [DataMember(Name = "details")]
        public CommonContent Details { get; set; }


        public override string ToString()
        {
            return $"Impact: {Details} ({Script})";
        }

        public override bool Equals(object? obj)
        {
            return obj is Impact other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Details.GetHashCode() ^ Script.GetHashCode();
        }

        public bool Equals(Impact other)
        {
            return other.Details.Equals(Details) && other.Script == Script;
        }
    }
}