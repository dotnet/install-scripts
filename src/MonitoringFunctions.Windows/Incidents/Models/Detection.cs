// Copyright (c) Microsoft. All rights reserved.

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// Manners in which an incident is discovered.
    /// </summary>
    internal enum Detection
    {
        /// <summary>
        /// Customer reported incident.
        /// </summary>
        Customer,
        /// <summary>
        /// Incident detected via monitoring.
        /// </summary>
        Monitoring,
        /// <summary>
        /// Incident detected in some other manner.
        /// </summary>
        Other
    }
}