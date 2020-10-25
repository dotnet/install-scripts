// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// A single incident root-cause.
    /// </summary>
    [DataContract]
    internal struct RootCause : IEquatable<RootCause>
    {
        /// <summary>
        /// <see cref="CommonContent"/> describing an incident root-cause.
        /// </summary>
        [DataMember(Name = "details")]
        public CommonContent Details { get; set; }


        public override string ToString()
        {
            return $"Root-Cause: {Details}";
        }

        public override bool Equals(object? obj)
        {
            return obj is RootCause other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Details.GetHashCode();
        }

        public bool Equals(RootCause other)
        {
            return other.Details.Equals(Details);
        }
    }
}