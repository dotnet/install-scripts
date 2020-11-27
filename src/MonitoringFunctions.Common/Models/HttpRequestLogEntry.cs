// Copyright (c) Microsoft. All rights reserved.

using MonitoringFunctions.DataService.Kusto;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MonitoringFunctions.Models
{
    /// <summary>
    /// Represents an http request event made from a function to be inserted into Kusto.
    /// </summary>
    internal struct HttpRequestLogEntry : IEquatable<HttpRequestLogEntry>, IKustoTableRow
    {
        /// <summary>
        /// Identifier for the monitor that generated this log entry.
        /// </summary>
        [JsonProperty("monitor_name"), JsonRequired]
        public string? MonitorName { get; set; }

        /// <summary>
        /// The time that the http request was made.
        /// </summary>
        [JsonProperty("timestamp"), JsonRequired]
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Url
        /// </summary>
        [JsonProperty("requested_url"), JsonRequired]
        public string? RequestedUrl { get; set; }

        /// <summary>
        /// Http response code returned by the server.
        /// The value is null if server couldn't be reached.
        /// </summary>
        [JsonProperty("http_response_code")]
        public int? HttpResponseCode { get; set; }

        /// <summary>
        /// Details of the error. This field can be used to determine the issue, in the case that 
        /// an HttpResponseCode couldn't be retrieved because the server is unreachable.
        /// </summary>
        [JsonProperty("error")]
        public string? Error { get; set; }

        public bool Equals([AllowNull] HttpRequestLogEntry other)
        {
            return MonitorName == other.MonitorName &&
                EventTime == other.EventTime &&
                RequestedUrl == other.RequestedUrl &&
                HttpResponseCode == other.HttpResponseCode &&
                Error == other.Error;
        }

        public override string? ToString()
        {
            return $"HttpRequestLogEntry - " +
                $"MonitorName: {MonitorName}" +
                $", EventTime: {EventTime}" +
                $", HttpResponseCode: {HttpResponseCode}" +
                $", RequestedUrl: {RequestedUrl}" +
                $", Error: {Error}";
        }
    }
}
