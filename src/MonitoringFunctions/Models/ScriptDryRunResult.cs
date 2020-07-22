// Copyright (c) Microsoft. All rights reserved.

namespace MonitoringFunctions.Models
{
    internal sealed class ScriptDryRunResult
    {
        public string? PrimaryUrl { get; set; }
        public string? LegacyUrl { get; set; }

        public override string? ToString()
        {
            return $"ScriptDryRunResult - PrimaryUrl: {PrimaryUrl}, LegacyUrl: {LegacyUrl}";
        }
    }
}
