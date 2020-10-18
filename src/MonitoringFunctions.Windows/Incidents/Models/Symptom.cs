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
        /// <see cref="string"/> symptom description.
        /// </summary>
        [DataMember(Name = "symptom", IsRequired = true)]
        public string? Item { get; set; }


        public override string ToString()
        {
            return Item ?? string.Empty;
        }

        public override bool Equals(object? obj)
        {
            return obj is Symptom other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Item?.GetHashCode() ?? 0;
        }

        public bool Equals(Symptom other)
        {
            return other.Item == Item;
        }
    }
}