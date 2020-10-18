// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// Defines state required for incident BI.
    /// </summary>
    [DataContract]
    internal struct Incident : IEquatable<Incident>
    {
        /// <summary>
        /// <see cref="string"/> name of the mointor, in cases where <see cref="Incident.HowDetected"/> is <see cref="Detection.Monitoring"/>.
        /// </summary>
        [DataMember(Name = "monitor-name")]
        public string MonitorName { get; set; }

        /// <summary>
        /// <see cref="List{Symptom}"/> describing incident symptoms.
        /// </summary>
        [DataMember(Name = "symptoms")]
        public List<Symptom>? Symptoms { get; set; }

        /// <summary>
        /// <see cref="Detection"/> detailing how the incident was detected.
        /// </summary>
        [DataMember(Name = "how-detected")]
        public Detection HowDetected { get; set; }

        /// <summary>
        /// <see cref="List{Impact}"/> describing incident impacts.
        /// </summary>
        [DataMember(Name = "impact")]
        public List<Impact>? Impacts { get; set; }

        /// <summary>
        /// <see cref="List{RootCause}"/> describing incident root-causes.
        /// </summary>
        [DataMember(Name = "root-cause")]
        public List<RootCause>? RootCauses { get; set; }

        /// <summary>
        /// <see cref="List{RecoveryStep}"/> describing incident recovery steps.
        /// </summary>
        [DataMember(Name = "recovery-steps")]
        public List<RecoveryStep>? RecoverySteps { get; set; }

        /// <summary>
        /// <see cref="DateTime"/> detailing when the incident began.
        /// </summary>
        [DataMember(Name = "occurrence-date")]
        public DateTime OcurrenceDate { get; set; }

        /// <summary>
        /// <see cref="DateTime"/> detailing when the incident was considered resolved.
        /// </summary>
        [DataMember(Name = "recovery-date")]
        public DateTime RecoveryDate { get; set; }


        public override string ToString()
        {
            return $"{Symptoms?.Count ?? 0} symptoms";
        }

        public override bool Equals(object? obj)
        {
            return obj is Incident other && Equals(other);
        }

        public override int GetHashCode()
        {
            return RecoveryDate.GetHashCode()
                ^ HowDetected.GetHashCode()
                ^ Impacts?.GetHashCode() ?? 0
                ^ OcurrenceDate.GetHashCode()
                ^ RecoverySteps?.GetHashCode() ?? 0
                ^ RootCauses?.GetHashCode() ?? 0
                ^ Symptoms?.GetHashCode() ?? 0
                ^ MonitorName?.GetHashCode() ?? 0;
        }

        public bool Equals(Incident other)
        {
            return other.RecoveryDate == RecoveryDate
                && other.HowDetected == HowDetected
                && other.Impacts == Impacts
                && other.OcurrenceDate == OcurrenceDate
                && other.RecoverySteps == RecoverySteps
                && other.RootCauses == RootCauses
                && other.Symptoms == Symptoms
                && other.MonitorName == MonitorName;
        }
    }
}