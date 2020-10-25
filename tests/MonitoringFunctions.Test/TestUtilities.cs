// Copyright (c) Microsoft. All rights reserved.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MonitoringFunctions.Incidents.Models;
using MonitoringFunctions.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonitoringFunctions.Test
{
    internal static class TestUtilities
    {
        internal static string GetJsonContent(string assetFileName)
        {
            string jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", assetFileName);
            Assert.IsTrue(File.Exists(jsonPath), $"Inexistent path {jsonPath}");
            string incidentJson = File.ReadAllText(jsonPath);
            Assert.IsFalse(string.IsNullOrWhiteSpace(incidentJson), "Failed to read incident json");
            return incidentJson;
        }

        internal static AlertNotificationData DeserializeAlertNotificationDataInstance()
        {
            string grafanaJson = GetJsonContent("SampleAlertNotificationData.json");
            AlertNotificationData grafanaInfo = JsonConvert.DeserializeObject<AlertNotificationData>(grafanaJson);
            Assert.IsNotNull(grafanaInfo);
            Assert.AreEqual(grafanaInfo.Message, "Some operations returned an unsuccessful http response code");
            return grafanaInfo;
        }

        internal static void AssertIncidentDescription(string description)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(description));
            Assert.IsFalse(string.IsNullOrWhiteSpace(description), "serialization of incident state failed");
        }

        internal static void AssertIncident(Incident incident)
        {
            Assert.IsNotNull(incident);
            Assert.IsNotNull(incident.Symptoms);
            Assert.IsTrue(incident.Symptoms.Count > 0, $"unexpected # of symptoms, {incident.Symptoms.Count}");
            Assert.IsTrue(incident.Symptoms[0].Details.Description.Contains("Some operations returned an unsuccessful http response code"));
            Assert.AreEqual(incident.HowDetected, Detection.Monitoring, $"Expected {Detection.Monitoring}, received {incident.HowDetected}");
        }

        internal static Incident CreateIncident(string symptom)
        {
            return new Incident
            {
                MonitorName = "test monitor",
                HowDetected = Detection.Monitoring,
                Symptoms = new List<Symptom>
                {
                    new Symptom
                    {
                        Details = new CommonContent
                        {
                            Description = "symptom",
                            Labels = new string[]
                            {
                                "symptom label"
                            }
                        }
                    },
                    new Symptom
                    {
                        Details = new CommonContent
                        {
                            Description = "symptom2",
                            Labels = new string[]
                            {
                                "symptom label2"
                            }
                        }
                    }
                },
                Impacts = new List<Impact>
                {
                    new Impact
                    {
                        Script = ImpactedScript.Both,
                        Details = new CommonContent
                        {
                            Description = "impact desc 2",
                            Labels = new string[]
                            {
                                "impact label 1"
                            }
                        }
                    },
                    new Impact
                    {
                        Script = ImpactedScript.PS1,
                        Details = new CommonContent
                        {
                            Description = "impact desc 2",
                            Labels = new string[]
                            {
                                "impact label 1"
                            }
                        }
                    }
                },
                RecoverySteps = new List<RecoveryStep>
                {
                    new RecoveryStep
                    {
                        RequiredCodeChange = false,
                        Details = new CommonContent
                        {
                            Description = "recovery step",
                            Labels = new string[]
                            {
                                "recovery step label"
                            }
                        }
                    }
                },
                RootCauses = new List<RootCause>
                {
                    new RootCause
                    {
                        Details = new CommonContent
                        {
                            Description = "root-cause",
                            Labels = new string[]
                            {
                                "root-cause label"
                            }
                        }
                    }
                },
                RecoveryDate = DateTime.UtcNow,
                OcurrenceDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3.5))
            };
        }
    }
}