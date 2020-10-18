// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.Incidents.Models;
using Newtonsoft.Json;

namespace MonitoringFunctions.Incidents.Serialization.Providers.Json
{
    /// <summary>
    /// Newtonsoft based <see cref="Serializer"/> provider.
    /// </summary>
    internal sealed class JsonSerializer : Serializer
    {
        protected override string Serialize(Incident incident)
        {
            return JsonConvert.SerializeObject(incident, Formatting.Indented);
        }

        protected override Incident Deserialize(string boxedIncident)
        {
            return JsonConvert.DeserializeObject<Incident>(boxedIncident);
        }
    }
}