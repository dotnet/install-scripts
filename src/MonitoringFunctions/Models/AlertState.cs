// Copyright (c) Microsoft. All rights reserved.

using System.Runtime.Serialization;

namespace MonitoringFunctions.Models
{
    internal enum AlertState
    {
        [EnumMember(Value = "ok")]
        Ok = 0,

        [EnumMember(Value = "paused")]
        Paused = 1,

        [EnumMember(Value = "alerting")]
        Alerting = 2,
        
        [EnumMember(Value = "pending")]
        Pending = 3,
        
        [EnumMember(Value = "no_data")]
        NoData = 4,
    }
}
