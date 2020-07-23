// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics.CodeAnalysis;

namespace MonitoringFunctions.Models
{
    internal struct ScriptDryRunResult : IEquatable<ScriptDryRunResult>
    {
        public string? PrimaryUrl { get; set; }
        public string? LegacyUrl { get; set; }

        public bool Equals([AllowNull] ScriptDryRunResult other)
        {
            return PrimaryUrl == other.PrimaryUrl &&
                LegacyUrl == other.LegacyUrl;
        }

        public override string? ToString()
        {
            return $"ScriptDryRunResult - PrimaryUrl: {PrimaryUrl}, LegacyUrl: {LegacyUrl}";
        }
    }
}
