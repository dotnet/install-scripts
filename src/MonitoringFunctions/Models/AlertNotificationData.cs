// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Stores the data arriving from a notification that was created as a result of a triggering alert in Grafana.
    /// </summary>
    internal struct AlertNotificationData : IEquatable<AlertNotificationData>
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
        public AlertEvaluation?[]? MatchingAlerts { get; set; }

        public bool Equals(AlertNotificationData other)
        {
            bool equals = Title == other.Title &&
                Message == other.Message &&
                OrganisationId == other.OrganisationId &&
                PanelId == other.PanelId &&
                DashboardId == other.DashboardId &&
                RuleId == other.RuleId &&
                RuleName == other.RuleName &&
                RuleUrl == other.RuleUrl &&
                State == other.State;

            if (!equals)
            {
                return false;
            }

            if (MatchingAlerts == other.MatchingAlerts)
            {
                return true;
            }

            if (MatchingAlerts == null || other.MatchingAlerts == null)
            {
                return false;
            }

            return MatchingAlerts.SequenceEqual(other.MatchingAlerts);
        }

        public override string ToString()
        {
            return $"Alert State: {State} - Message: {Message}";
        }
    }
}
