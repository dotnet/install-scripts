// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MonitoringFunctions.Incidents.Models
{
    [DataContract]
    internal struct CommonContent
    {
        /// <summary>
        /// <see cref="string"/> free hand description.
        /// </summary>
        [DataMember(Name = "description")]
        public string? Description { get; set; }

        [DataMember(Name = "labels")]
        public string[] Labels { get; set; }


        public override string ToString()
        {
            return $"{Description ?? string.Empty} | Labels: {string.Join(",", Labels ?? new string[] { })}";
        }

        public override int GetHashCode()
        {
            return Description?.GetHashCode() ?? 0 ^ GetArrayHashCode(Labels);
        }

        public override bool Equals(object? obj)
        {
            return obj is CommonContent other && Equals(other);
        }

        public bool Equals(CommonContent other)
        {
            return other.Description == Description && AreEqual(other.Labels, Labels);
        }


        private static bool AreEqual(string[] models, string[] otherModels)
        {
            if (models == null || models.Length < 1)
            {
                return otherModels == null || otherModels.Length < 1;
            }
            if (otherModels == null || otherModels.Length < 1)
            {
                return false;
            }

            if (models.Length != otherModels.Length)
            {
                return false;
            }

            foreach (string item in models)
            {
                if (!otherModels.Contains(item))
                {
                    return false;
                }
            }
            return true;
        }

        public static int GetArrayHashCode(string[] models)
        {
            int hashCode = 0;
            foreach (string item in models)
            {
                hashCode ^= item.GetHashCode();
            }
            return hashCode;
        }
    }
}