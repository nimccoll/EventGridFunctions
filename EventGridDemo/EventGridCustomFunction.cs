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
// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using EventGridDemo.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System;

namespace EventGridDemo
{
    public static class EventGridCustomFunction
    {
        private static readonly string TABLE_NAME = "Orders";

        [FunctionName("EventGridCustomFunction")]
        public static void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            // Parse the partition key and row key from the Subject of the Event Grid event
            string[] tableKeys = eventGridEvent.Subject.Split(":");
            log.LogInformation($"Retrieving order with partition key {tableKeys[0]} and row key {tableKeys[1]}");

            // Retrieve the order from Azure Table Storage
            CloudStorageAccount cloudStorageAccount;
            CloudTableClient cloudTableClient;
            string tableStorageConnectionString = Environment.GetEnvironmentVariable("TableStorageConnectionString", EnvironmentVariableTarget.Process);
            cloudStorageAccount = CloudStorageAccount.Parse(tableStorageConnectionString);
            cloudTableClient = cloudStorageAccount.CreateCloudTableClient(new TableClientConfiguration());
            CloudTable orders = cloudTableClient.GetTableReference(TABLE_NAME);
            bool exists = orders.Exists();

            if (exists)
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<OrderEntity>(tableKeys[0], tableKeys[1]);
                TableResult result = orders.Execute(retrieveOperation);
                OrderEntity order = result.Result as OrderEntity;
                if (order != null)
                {
                    log.LogInformation($"Order {order.OrderId} for customer {order.CustomerId} successfully retrieved.");
                }
            }
        }
    }
}
