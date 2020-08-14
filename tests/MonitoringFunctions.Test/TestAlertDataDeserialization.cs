// Copyright (c) Microsoft. All rights reserved.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringFunctions.Models;
using Newtonsoft.Json;
using System.IO;

namespace MonitoringFunctions.Test
{
    [TestClass]
    public class TestAlertDataDeserialization
    {
        [TestMethod]
        public void TestDataDeserialization()
        {
            string jsonData = File.ReadAllText("Assets/SampleAlertNotificationData.json");

            _ = JsonConvert.DeserializeObject<AlertNotificationData>(jsonData);
        }
    }
}
