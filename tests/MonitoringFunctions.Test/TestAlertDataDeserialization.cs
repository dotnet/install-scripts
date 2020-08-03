// Copyright (c) Microsoft. All rights reserved.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringFunctions.Models;
using Newtonsoft.Json;
using System;
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

            try
            {
                AlertNotificationData data = JsonConvert.DeserializeObject<AlertNotificationData>(jsonData);
            }
            catch(JsonException e)
            {
                Assert.Fail($"Notification deserialization has failed with exception:{Environment.NewLine}{e.ToString()}");
            }
        }
    }
}
