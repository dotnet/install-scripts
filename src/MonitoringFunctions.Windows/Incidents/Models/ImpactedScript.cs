// Copyright (c) Microsoft. All rights reserved.

namespace MonitoringFunctions.Incidents.Models
{
    /// <summary>
    /// Script experiencing a given incident impact.
    /// </summary>
    internal enum ImpactedScript
    {
        /// <summary>
        /// PowerShell script only.
        /// </summary>
        PS1,
        /// <summary>
        /// Shell script only.
        /// </summary>
        SH,
        /// <summary>
        /// Both PowerShell and Shell scripts.
        /// </summary>
        Both,
        /// <summary>
        /// Neither PowerShell nor Shell scripts.
        /// </summary>
        Neither
    }
}