// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// An individual incident recovery step.
    /// </summary>
    [DataContract]
    internal struct RecoveryStep : IEquatable<RecoveryStep>
    {
        /// <summary>
        /// <see cref="string"/> describing the recovery step.
        /// </summary>
        [DataMember(Name = "recovery-step")]
        public string? Step { get; set; }

        /// <summary>
        /// True if the recovery step required code changes, false otherwise.
        /// </summary>
        [DataMember(Name = "code-changes")]
        public bool RequiredCodeChange { get; set; }

 
        public override string ToString()
        {
            return $"Recovery Step: {Step ?? string.Empty} (Required code change? {RequiredCodeChange})";
        }

        public override bool Equals(object? obj)
        {
            return obj is RecoveryStep other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Step?.GetHashCode() ?? 0) ^ RequiredCodeChange.GetHashCode();
        }

        public bool Equals(RecoveryStep other)
        {
            return other.Step == Step && other.RequiredCodeChange == RequiredCodeChange;
        }
    }
}