// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.Incidents.Models;
using MonitoringFunctions.Incidents.Serialization;
using MonitoringFunctions.Incidents.Serialization.Providers.Json;
using MonitoringFunctions.Models;
using System;
using System.Collections.Generic;

namespace MonitoringFunctions.Incidents
{
    /// <summary>
    /// API to interact with incident serialization.
    /// </summary>
    internal sealed class IncidentSerializer
    {
        private readonly Serializer _serializer;


        /// <summary>
        /// Creates instances relying on a JSON <see cref="Serializer"/> implementation.
        /// </summary>
        public IncidentSerializer()
            : this(new JsonSerializer())
        {
        }

        /// <summary>
        /// Creates instances relying on the specified <see cref="Serializer"/>.
        /// </summary>
        /// <param name="serializer">Specified <see cref="Serializer"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if the specified <see cref="Serializer"/> is null.</exception>
        public IncidentSerializer(Serializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(paramName: nameof(serializer));
        }


        /// <summary>
        /// Serializes an <see cref="Incident"/> instance based on the specified <see cref="AlertNotificationData"/> instance.
        /// </summary>
        /// <param name="monitorName"><see cref="string"/> name of relevant monitor.</param>
        /// <param name="data">Specified <see cref="AlertNotificationData"/> instance.</param>
        /// <returns><see cref="string"/> representing serialized <see cref="Incident"/></returns>
        public string GetIncidentDescription(string monitorName, AlertNotificationData data)
        {
            Incident incident = new Incident
            {
                MonitorName = monitorName,
                OcurrenceDate = DateTime.UtcNow,
                HowDetected = Detection.Monitoring,
                Symptoms = new List<Symptom>
                {
                    new Symptom
                    {
                        Details = new CommonContent
                        {
                            Description = $"{data.Message ?? string.Empty}",
                            Labels = new string[]
                            {
                                "monitoring failure"
                            }
                        }
                    }
                }
            };

            return _serializer.GetString(incident);
        }

        /// <summary>
        /// Attempts to deserialize the specified <see cref="string"/> into an <see cref="Incident"/> instance.
        /// </summary>
        /// <param name="boxedIncident">Specified boxed <see cref="Incident"/> <see cref="string"/>.</param>
        /// <returns><see cref="Incident"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="boxedIncident"/> is null or empty string.</exception>
        public Incident GetIncident(string boxedIncident)
        {
            return string.IsNullOrWhiteSpace(boxedIncident) ? throw new ArgumentNullException(paramName:nameof(boxedIncident)) : _serializer.GetIncident(boxedIncident);
        }
    }
}