// Copyright (c) Microsoft. All rights reserved.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using MonitoringFunctions.Incidents;
using MonitoringFunctions.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions.Windows.Functions
{
    internal static class WorkItemCreator
    {
        private const string ProjectName = "devdiv";
        private const string IncidentAreaPath = "DevDiv\\NET Tools\\install-scripts-incidents";
        private static readonly string? _devdivAdoPAT = Environment.GetEnvironmentVariable("devdiv-ado-pat");
        private static readonly Uri _devdivCollectionUri = new Uri("https://devdiv.visualstudio.com/DefaultCollection/");
        private static readonly IncidentSerializer IncidentSerializer = new IncidentSerializer();

        [FunctionName("WorkItemCreator")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            using StreamReader requestStream = new StreamReader(req.Body);
            string requestBody = await requestStream.ReadToEndAsync().ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(requestBody))
            {
                throw new InvalidOperationException("Request body shouldn't be empty");
            }

            AlertNotificationData data = JsonConvert.DeserializeObject<AlertNotificationData>(requestBody);

            if (data.State != AlertState.Alerting)
            {
                throw new ArgumentOutOfRangeException("Alert state is not \"Alerting\". This notification shouldn't have been sent to this function.");
            }

            VssCredentials credentials = new VssBasicCredential("", _devdivAdoPAT);
            WorkItemTrackingHttpClient workItemClient = new WorkItemTrackingHttpClient(_devdivCollectionUri, credentials);
            ProjectHttpClient projectClient = new ProjectHttpClient(_devdivCollectionUri, credentials);
            TeamProject devdivProject = await projectClient.GetProject(ProjectName).ConfigureAwait(false);

            if (devdivProject == null)
            {
                throw new ProjectDoesNotExistException($"The project \"{ProjectName}\" was not found or you do not have permission to access it.");
            }

            string alertMessage = data.Message ?? "Alert triggered";
            int alertingMonitorCount = data.MatchingAlerts?.Length ?? 0;
            List<WorkItem> createdWorkItems = new List<WorkItem>();

            for (int i = 0; i < alertingMonitorCount; i++)
            {
                AlertEvaluation? match = data.MatchingAlerts![i];

                if (match == null || match.Value.Tags == null)
                {
                    log.LogError($"Evaluationg match #{i} has invalid data.");
                    continue;
                }

                if (!match.Value.Tags.ContainsKey("monitor_name"))
                {
                    log.LogError($"Evaluation match #{i} doesn't specify a monitor name which is required.");
                    continue;
                }

                string title = $"#{match.Value.Tags["monitor_name"]}# {alertMessage}";

                bool workItemExists = await ActiveWorkItemExists(workItemClient, devdivProject, title, IncidentAreaPath)
                    .ConfigureAwait(false);

                if (workItemExists)
                {
                    log.LogInformation("There is already a work item tracking this issue. A new work item will not be created.");
                    continue;
                }

                string description = IncidentSerializer.GetIncidentDescription(match.Value.Tags["monitor_name"], data, log);
                log.LogInformation($"Incident description body: {description}");
                WorkItem workItem = await CreateAdoTask(workItemClient, devdivProject, IncidentAreaPath, title, description).ConfigureAwait(false);

                string workItemUrl = (workItem.Links?.Links?["html"] as ReferenceLink)?.Href ?? "<url-not-found>";
                string successMessage = $"Work item with ID {workItem.Id} was created at address {workItemUrl}";

                log.LogInformation(successMessage);
                createdWorkItems.Add(workItem);
            }

            return new OkObjectResult($"{createdWorkItems.Count} work items were created with IDs {string.Join(", ", createdWorkItems.Select(w => w.Id))}");
        }

        /// <summary>
        /// Searches for an active work item in the given project with the given title.
        /// </summary>
        /// <param name="project">Project to search for the work item.</param>
        /// <param name="title">Title of the work item.</param>
        /// <param name="areaPath">Area path of the work item.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if a work item is found. False otherwise.</returns>
        private static async Task<bool> ActiveWorkItemExists(
            WorkItemTrackingHttpClient workItemClient,
            TeamProject project,
            string title,
            string areaPath,
            CancellationToken cancellationToken = default)
        {
            string wiqlQuery = $@"SELECT
                    [System.Id]
                FROM workitems
                WHERE
                    [System.Title] = '{title.Replace('\'', '_')}'
                    AND [System.AreaPath] = '{areaPath.Replace('\'', '_')}'
                    AND NOT [System.State] IN ('6 - Closed', 'Closed', 'Resolved', 'Cut', 'Completed')";

            WorkItemQueryResult queryResult = await workItemClient.QueryByWiqlAsync(new Wiql() { Query = wiqlQuery }, project.Id,
                null, null, null, cancellationToken).ConfigureAwait(false);

            return queryResult.WorkItems.Any();
        }

        /// <summary>
        /// Creates a bug in ADO with given fields.
        /// </summary>
        /// <param name="workItemClient">Client to use for creating the work item.</param>
        /// <param name="project">ADO project to create the work item in</param>
        /// <param name="areaPath">AreaPath of the work item.</param>
        /// <param name="title">Title of the work item.</param>
        /// <param name="description">Description of the work item.</param>
        /// <param name="tags">Comma separated tags of the work item.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The created work item</returns>
        private static async Task<WorkItem> CreateAdoTask(
            WorkItemTrackingHttpClient workItemClient,
            TeamProject project,
            string areaPath,
            string title,
            string? description = null,
            string? tags = null,
            CancellationToken cancellationToken = default)
        {
            JsonPatchDocument workItemJson = GetJsonForNewWorkItem(areaPath, title, description, tags);
            WorkItem generatedBug = await workItemClient.CreateWorkItemAsync(workItemJson, project.Id, "Task",
                null, null, null, null, null, cancellationToken).ConfigureAwait(false);

            if (generatedBug == null)
            {
                throw new Exception("Failed to create a new work item.");
            }

            return generatedBug;
        }

        /// <summary>
        /// Creates a <see cref="JsonPatchDocument"/> for creating a work item with the given fields.
        /// </summary>
        /// <param name="areaPath">AreaPath of the work item.</param>
        /// <param name="title">Title of the work item.</param>
        /// <param name="description">Description of the work item.</param>
        /// <param name="tags">Comma separated tags of the work item.</param>
        /// <returns><see cref="JsonPatchDocument"/> that defines a work item.</returns>
        private static JsonPatchDocument GetJsonForNewWorkItem(
            string areaPath,
            string title,
            string? description = null,
            string? tags = null)
        {
            JsonPatchDocument jDocument = new JsonPatchDocument();

            jDocument.Add(new JsonPatchOperation()
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.AreaPath",
                Value = areaPath
            });

            jDocument.Add(new JsonPatchOperation()
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.Title",
                Value = title
            });

            jDocument.Add(new JsonPatchOperation()
            {
                Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                Path = "/fields/System.Description",
                Value = description
            });

            if (tags != null)
            {
                jDocument.Add(new JsonPatchOperation()
                {
                    Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
                    Path = "/fields/System.Tags",
                    Value = tags
                });
            }

            return jDocument;
        }
    }
}
