// Copyright (c) Microsoft. All rights reserved.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringFunctions.Incidents;
using MonitoringFunctions.Incidents.Models;
using MonitoringFunctions.Incidents.Serialization.Providers.Json;
using MonitoringFunctions.Models;

namespace MonitoringFunctions.Test
{
    /// <summary>
    /// Unit tests for the <see cref="IncidentSerializer"/> class &amp; descendants.
    /// </summary>
    [TestClass]
    public class TestIncidentSerializer
    {
        private const string NewIncidentJson = "SampleNewIncident.json", ResolvedIncidentJson = "SampleResolvedIncident.json";

        /// <summary>
        /// Verifies incident content serialization as it happens in incident record creation.
        /// </summary>
        [TestMethod]
        public void TestNominativeSerialization()
        {
            AlertNotificationData grafanaInfo = TestUtilities.DeserializeAlertNotificationDataInstance();
            IncidentSerializer jsonSerializer = new IncidentSerializer();
            Assert.IsNotNull(jsonSerializer);
            TestUtilities.AssertIncidentDescription(jsonSerializer.GetIncidentDescription("test alert", grafanaInfo));
        }

        /// <summary>
        /// Verifies incident state deserialization, enabling future BI use cases.
        /// </summary>
        /// <param name="jsonFile">json file detailing the type of incident to process.</param>
        [TestMethod]
        [DataRow(NewIncidentJson)]
        [DataRow(ResolvedIncidentJson)]
        public void TestNominativeDeserialization(string jsonFile)
        {
            string incidentJson = TestUtilities.GetJsonContent(jsonFile);
            Incident incident = new IncidentSerializer().GetIncident(incidentJson);
            TestUtilities.AssertIncident(incident);
        }

        /// <summary>
        /// Verifies incident content serialization as it hapens in incident record creation in the JSON serializer.
        /// </summary>
        [TestMethod]
        public void TestNominativeJsonSerializerSerialization()
        {
            AlertNotificationData grafanaInfo = TestUtilities.DeserializeAlertNotificationDataInstance();
            Incident incident = TestUtilities.CreateIncident(grafanaInfo.Message);
            JsonSerializer jsonSerializer = new JsonSerializer();
            Assert.IsNotNull(jsonSerializer);
            TestUtilities.AssertIncidentDescription(jsonSerializer.GetString(incident));
        }

        /// <summary>
        /// Verifies incident state deserialization in the JSON serializer, enabling future BI use cases.
        /// </summary>
        [TestMethod]
        public void TestNominativeJsonSerializerDeserialization()
        {
            JsonSerializer jsonSerializer = new JsonSerializer();
            Assert.IsNotNull(jsonSerializer);
            string incidentJson = TestUtilities.GetJsonContent(NewIncidentJson);
            Incident incident = jsonSerializer.GetIncident(incidentJson);
            TestUtilities.AssertIncident(incident);
        }
    }
}