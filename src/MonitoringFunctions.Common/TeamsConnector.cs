// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MonitoringFunctions.Common
{
    /// <summary>
    /// Represents a connector for MS Teams.
    /// </summary>
    internal sealed class TeamsConnector
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private const string IncidentCardFormat = "{{\"@context\":\"https://schema.org/extensions\",\"@type\":\"MessageCard\",\"themeColor\":\"FF5555\",\"title\":\"{0}\",\"text\":\"{1}\",\"potentialAction\":[{{\"@type\":\"OpenUri\",\"name\":\"Go To Work Item\",\"targets\":[{{\"os\":\"default\",\"uri\":\"{2}\"}}]}}]}}";
        
        private string _webhookUrl;

        /// <summary>
        /// Creates a new instance of type <see cref="TeamsConnector"/>.
        /// </summary>
        /// <param name="webhookUrl">Webhook URL that can be used to access this connector on Teams.</param>
        /// <exception cref="ArgumentException">If the supplied webhook url is null or whitespace.</exception>
        public TeamsConnector(string webhookUrl)
        {
            if(string.IsNullOrWhiteSpace(webhookUrl))
            {
                throw new ArgumentException($"{webhookUrl} can not be null or empty.");
            }

            _webhookUrl = webhookUrl;
        }

        /// <summary>
        /// Attempts to send an incident card to the teams channel linked to the webhook URL.
        /// </summary>
        /// <param name="title">Title of the card.</param>
        /// <param name="description">Description text on the card.</param>
        /// <param name="workItemUrl">Link to the work item of the incident.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>Task that tracks the completion of the operation.
        /// Errors are communicated through exceptions.</returns>
        /// <remarks>It is safe to call this method on the same object simultaneously from multiple threads.</remarks>
        public async Task SendIncidentCard(string title, string description, string workItemUrl, CancellationToken cancellationToken = default)
        {
            string payload = string.Format(IncidentCardFormat, title, description, workItemUrl);
            using StringContent payloadContent = new StringContent(payload, Encoding.UTF8);

            using HttpResponseMessage response = await HttpClient.PostAsync(_webhookUrl, payloadContent, cancellationToken);

            if(!response.IsSuccessStatusCode)
            {
                throw new Exception($"Sending incident card has failed with http status code {response.StatusCode}.");
            }
        }
    }
}
