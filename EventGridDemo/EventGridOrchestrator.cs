//===============================================================================
// Microsoft FastTrack for Azure
// Azure Event Grid and Service Bus Triggered Functions
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace EventGridDemo
{
    public static class EventGridOrchestrator
    {
        [FunctionName("EventGridOrchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("EventGridOrchestrator_Hello", "Tokyo"));
            outputs.Add(await context.CallActivityAsync<string>("EventGridOrchestrator_Hello", "Seattle"));
            outputs.Add(await context.CallActivityAsync<string>("EventGridOrchestrator_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("EventGridOrchestrator_Hello")]
        public static string SayHello([ActivityTrigger] string name, ILogger log)
        {
            //log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("EventGridOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            string requestContent = await req.Content.ReadAsStringAsync();
            log.LogInformation(requestContent);

            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("EventGridOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("EventGridOrchestrator_ServiceBusStart")]
        public static async Task ServiceBusStart([ServiceBusTrigger("eventgriddemo", "eventgriddemosubsession", Connection = "serviceBusConnString", IsSessionsEnabled = true)] string mySbMsg,
            int deliveryCount,
            DateTime enqueuedTimeUtc,
            string messageId,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            string instanceId = await starter.StartNewAsync("EventGridOrchestrator", null);

            log.LogInformation($"MessageId: {messageId} Message: {mySbMsg}");
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}