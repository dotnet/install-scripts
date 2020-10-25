// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Runtime.Serialization;

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// An individual incident symptom.
    /// </summary>
    [DataContract]
    internal struct Symptom : IEquatable<Symptom>
    {
        /// <summary>
        /// <see cref="CommonContent"/> describing symptom.
        /// </summary>
        [DataMember(Name = "details")]
        public CommonContent Details { get; set; }


        public override string ToString()
        {
            return Details.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is Symptom other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Details.GetHashCode();
        }

        public bool Equals(Symptom other)
        {
            return other.Details.Equals(Details);
        }
    }
}