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
using MonitoringFunctions.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions.Functions
{
    internal static class WorkItemCreator
    {
        private const string ProjectName = "devdiv";
        private static readonly string? _devdivAdoPAT = Environment.GetEnvironmentVariable("devdiv-ado-pat");
        private static readonly Uri _devdivCollectionUri = new Uri("https://devdiv.visualstudio.com/DefaultCollection/");

        [FunctionName("WorkItemCreator")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            List<WorkItem> createdWorkItems = new List<WorkItem>();

            using StreamReader requestStream = new StreamReader(req.Body);
            string requestBody = await requestStream.ReadToEndAsync().ConfigureAwait(false);

            if(string.IsNullOrWhiteSpace(requestBody))
            {
                return new BadRequestObjectResult("Request body shouldn't be empty");
            }

            AlertNotificationData data;
            try
            {
                data = JsonConvert.DeserializeObject<AlertNotificationData>(requestBody);
            }
            catch(JsonException e)
            {
                string errorMessage = "Deserialization of the request body has failed.";
                log.LogError(e, errorMessage);
                return new BadRequestObjectResult(errorMessage);
            }

            string alertMessage = data.Message ?? "Alert triggered";
            int alertingMonitorCount = data.MatchingAlerts?.Length ?? 0;

            if(data.State != AlertState.Alerting)
            {
                string errorMessage = "Alert state is not \"Alerting\". This notification shouldn't have been sent to this function.";
                log.LogError(errorMessage);
                return new BadRequestObjectResult(errorMessage);
            }

            for(int i=0; i<alertingMonitorCount; i++)
            {
                AlertEvaluation? match = data.MatchingAlerts![i];

                if(match == null || match.Value.Tags == null)
                {
                    log.LogError($"Evaluationg match #{i} has invalid data.");
                    continue;
                }

                if(!match.Value.Tags.ContainsKey("monitor_name"))
                {
                    log.LogError($"Evaluation match #{i} doesn't specify a monitor name which is required.");
                    continue;
                }

                string title = $"{match.Value.Tags["monitor_name"]} {alertMessage}";
                string description = $"Alert details:{Environment.NewLine}{requestBody}";
                WorkItem workItem = await CreateAdoTask("DevDiv\\NET Tools\\install-scripts-incidents", title, description).ConfigureAwait(false);

                string workItemUrl = (workItem.Links?.Links?["html"] as ReferenceLink)?.Href ?? "<url-not-found>";
                string successMessage = $"Work item with ID {workItem.Id} was created at address {workItemUrl}";
            
                log.LogInformation(successMessage);
                createdWorkItems.Add(workItem);
            }

            return new OkObjectResult($"{createdWorkItems.Count} work items were created with IDs {string.Join(", ", createdWorkItems.Select(w => w.Id))}");
        }

        /// <summary>
        /// Creates a bug in ADO with given fields.
        /// </summary>
        /// <param name="title">Title of the work item.</param>
        /// <param name="areaPath">AreaPath of the work item.</param>
        /// <param name="description">Description of the work item.</param>
        /// <param name="reproSteps">Reproduction steps of the work item. For work items with type "Bug", this field 
        /// shows up in ADO instead of <paramref name="description"/>.</param>
        /// <param name="tags">Comma separated tags of the work item.</param>
        /// <returns>The created work item</returns>
        private static async Task<WorkItem> CreateAdoTask(
            string areaPath,
            string title,
            string? description = null,
            string? tags = null,
            CancellationToken cancellationToken = default)
        {
            VssCredentials creds = new VssBasicCredential("", _devdivAdoPAT);
            ProjectHttpClient projectClient = new ProjectHttpClient(_devdivCollectionUri, creds);
            TeamProject devdivProject = await projectClient.GetProject(ProjectName).ConfigureAwait(false);

            if (devdivProject == null)
            {
                throw new ProjectDoesNotExistException($"The project \"{ProjectName}\" was not found or you do not have permission to access it.");
            }

            WorkItemTrackingHttpClient workItemClient = new WorkItemTrackingHttpClient(_devdivCollectionUri, creds);
            JsonPatchDocument workItemJson = GetJsonForNewWorkItem(areaPath, title, description, tags);
            WorkItem generatedBug = await workItemClient.CreateWorkItemAsync(workItemJson, devdivProject.Id, "Task",
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
        /// <param name="title">Title of the work item.</param>
        /// <param name="areaPath">AreaPath of the work item.</param>
        /// <param name="description">Description of the work item.</param>
        /// <param name="reproSteps">Reproduction steps of the work item. For work items with type "Bug", this field 
        /// shows up in ADO instead of <paramref name="description"/>.</param>
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
