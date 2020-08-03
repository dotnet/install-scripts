// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Stores the data arriving from a notification that was created as a result of a triggering alert in Grafana.
    /// </summary>
    internal sealed class AlertNotificationData
    {
        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("orgId")]
        public string? OrganisationId { get; set; }

        [JsonProperty("panelId")]
        public string? PanelId { get; set; }

        [JsonProperty("dashboardId")]
        public string? DashboardId { get; set; }

        [JsonProperty("ruleId")]
        public string? RuleId { get; set; }

        [JsonProperty("ruleName")]
        public string? RuleName { get; set; }

        [JsonProperty("ruleUrl")]
        public string? RuleUrl { get; set; }

        [JsonProperty("state")]
        public AlertState State { get; set; }

        [JsonProperty("tags")]
        public Dictionary<string, string>? Tags { get; set; }

        [JsonProperty("evalMatches")]
        public AlertEvaluation[]? MatchingAlerts { get; set; }
    }
}
