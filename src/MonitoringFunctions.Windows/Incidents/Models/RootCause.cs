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
        /// <see cref="RcCategory"/> detailing the root-cause's category.
        /// </summary>
        [DataMember(Name = "category")]
        public RcCategory Reason { get; set; }

        /// <summary>
        /// <see cref="string"/> describing the root-cause.
        /// </summary>
        [DataMember(Name = "cause")]
        public string? Cause { get; set; }


        public override string ToString()
        {
            return $"Root-Cause: ({Reason}) {Cause ?? string.Empty}";
        }

        public override bool Equals(object? obj)
        {
            return obj is RootCause other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Reason.GetHashCode() ^ (Cause?.GetHashCode() ?? 0);
        }

        public bool Equals(RootCause other)
        {
            return other.Reason == Reason && other.Cause == Cause;
        }
    }
}