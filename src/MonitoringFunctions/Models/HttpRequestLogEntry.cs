// Copyright (c) Microsoft. All rights reserved.

using Newtonsoft.Json;
using System;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Represents an http request event made from a function to be inserted into Kusto.
    /// </summary>
    internal class HttpRequestLogEntry
    {
        [JsonProperty("monitor_name"), JsonRequired]
        public string? MonitorName { get; set; }

        [JsonProperty("timestamp"), JsonRequired]
        public DateTime EventTime { get; set; }

        [JsonProperty("requested_url"), JsonRequired]
        public string? RequestedUrl { get; set; }

        [JsonProperty("http_status_code"), JsonRequired]
        public int? HttpStatusCode { get; set; }
    }
}
