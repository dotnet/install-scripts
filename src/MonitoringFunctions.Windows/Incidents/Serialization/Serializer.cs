// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.Incidents.Models;
using System;

namespace MonitoringFunctions.Incidents.Serialization
{
    /// <summary>
    /// Defines required incident serialization capabilities
    /// </summary>
    internal abstract class Serializer
    {
        /// <summary>
        /// Creates a <see cref="string"/> representative of the specified <see cref="Incident"/> instance.
        /// </summary>
        /// <param name="incident">Specified <see cref="Incident"/> instance.</param>
        /// <returns>Representative <see cref="string"/>.</returns>
        internal string GetString(Incident incident)
        {
            return Serialize(incident);
        }

        /// <summary>
        /// Creates an <see cref="Incident"/> instance from the specified <see cref="string"/>.
        /// </summary>
        /// <param name="boxedIncident">Specified <see cref="string"/>.</param>
        /// <returns>Corresponding <see cref="Incident"/> instance.</returns>
        internal Incident GetIncident(string boxedIncident)
        {
            return string.IsNullOrWhiteSpace(boxedIncident) ? throw new ArgumentNullException(paramName: nameof(boxedIncident)) : Deserialize(boxedIncident);
        }


        protected abstract string Serialize(Incident incident);

        protected abstract Incident Deserialize(string boxedIncident);
    }
}