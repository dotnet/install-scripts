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
        /// True if the recovery step required code changes, false otherwise.
        /// </summary>
        [DataMember(Name = "code-changes")]
        public bool RequiredCodeChange { get; set; }

        /// <summary>
        /// <see cref="CommonContent"/> describing an incident symptom.
        /// </summary>
        [DataMember(Name = "details")]
        public CommonContent Details { get; set; }


        public override string ToString()
        {
            return Details.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is RecoveryStep other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Details.GetHashCode() ^ RequiredCodeChange.GetHashCode();
        }

        public bool Equals(RecoveryStep other)
        {
            return other.Details.Equals(Details) && other.RequiredCodeChange == RequiredCodeChange;
        }
    }
}