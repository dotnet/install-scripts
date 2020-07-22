// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.DataService.Kusto;
using Newtonsoft.Json;
using System;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Represents an http request event made from a function to be inserted into Kusto.
    /// </summary>
    internal sealed class HttpRequestLogEntry : IKustoTableRow
    {
        [JsonProperty("monitor_name"), JsonRequired]
        public string? MonitorName { get; set; }

        [JsonProperty("timestamp"), JsonRequired]
        public DateTime EventTime { get; set; }

        [JsonProperty("requested_url"), JsonRequired]
        public string? RequestedUrl { get; set; }

        [JsonProperty("http_response_code"), JsonRequired]
        public int? HttpResponseCode { get; set; }

        public override string? ToString()
        {
            return $"HttpRequestLogEntry - MonitorName: {MonitorName}, EventTime: {EventTime}, HttpResponseCode: {HttpResponseCode}, RequestedUrl: {RequestedUrl}";
        }
    }
}
