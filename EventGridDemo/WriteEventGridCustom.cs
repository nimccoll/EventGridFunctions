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
using EventGridDemo.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;

namespace EventGridDemo
{
    public static class WriteEventGridCustom
    {
        private static readonly string TABLE_NAME = "Orders";

        [FunctionName("WriteEventGridCustom")]
        public static void Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Order creation timer trigger function executed at: {DateTime.Now}");

            // Create an order and write it to Azure Table Storage
            OrderEntity order = CreateOrder();
            CloudStorageAccount cloudStorageAccount;
            CloudTableClient cloudTableClient;
            string tableStorageConnectionString = Environment.GetEnvironmentVariable("TableStorageConnectionString", EnvironmentVariableTarget.Process);
            cloudStorageAccount = CloudStorageAccount.Parse(tableStorageConnectionString);
            cloudTableClient = cloudStorageAccount.CreateCloudTableClient(new TableClientConfiguration());
            CloudTable orders = cloudTableClient.GetTableReference(TABLE_NAME);
            bool exists = orders.Exists();

            if (exists)
            {
                TableOperation createOperation = TableOperation.Insert(order);
                TableResult result = orders.Execute(createOperation);

                // Send an order created event to the Azure Event Grid
                EventGridEvent eventGridEvent = new EventGridEvent()
                {
                    Id = Guid.NewGuid().ToString(),
                    Subject = $"{order.PartitionKey}:{order.RowKey}",
                    EventTime = DateTime.Now,
                    EventType = "OrderCreated"
                };
                EventGridEvent[] events = new EventGridEvent[] { eventGridEvent };
                string eventGridEndpoint = Environment.GetEnvironmentVariable("EventGridEndpoint");
                string eventGridKey = Environment.GetEnvironmentVariable("EventGridKey");
                HttpClient httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("aeg-sas-key", eventGridKey);
                HttpResponseMessage response = httpClient.PostAsJsonAsync(eventGridEndpoint, events).Result;
                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation($"Order {order.OrderId} created.");
                }
            }
        }

        private static OrderEntity CreateOrder()
        {
            Random random = new Random();

            OrderEntity order = new OrderEntity()
            {
                CustomerId = Guid.NewGuid().ToString(),
                OrderId = Guid.NewGuid().ToString(),
                ProductId = Guid.NewGuid().ToString(),
                Quantity = random.Next(1, 100)
            };

            return order;
        }
    }
}
