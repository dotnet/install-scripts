// Copyright (c) Microsoft. All rights reserved.

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// Available root-cause catetories.
    /// </summary>
    internal enum RcCategory
    {
        /// <summary>
        /// Incident culprit was a problem in the build agent running the script.
        /// </summary>
        BuildAgent,
        /// <summary>
        /// Install-script bug.
        /// </summary>
        Bug,
        /// <summary>
        /// Problem in the environment hostingthe agent running the script.
        /// </summary>
        Environment,
        /// <summary>
        /// Not a valid incident.
        /// </summary>
        FalseAlarm,
        /// <summary>
        /// Any networking problem responsible for the incident.
        /// </summary>
        Network,
        /// <summary>
        /// Root-cause other than the other categories.
        /// </summary>
        Other,
        /// <summary>
        /// Targeted SDK resource was unavailable in the feed.
        /// </summary>
        UnavailableResource,
        /// <summary>
        /// Scripts download website issues.
        /// </summary>
        Website,
    }
}