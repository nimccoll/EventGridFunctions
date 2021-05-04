using EventGridDemo.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;

namespace EventGridDemo
{
    public static class WriteEventGridCustom
    {
        private static readonly string TABLE_NAME = "Orders";
        private static readonly int _retryDelay = 300;
        private static readonly int _maximumRetries = 5;

        [FunctionName("WriteEventGridCustom")]
        public static void Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
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
                HttpResponseMessage response = PostTransient(eventGridEndpoint, eventGridKey, events);
                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation($"Order {order.OrderId} created.");
                }
                else
                {
                    log.LogError($"Write to event grid failed. Status Code is {response.StatusCode}");
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

        private static HttpResponseMessage PostTransient(string eventGridEndpoint, string eventGridKey, EventGridEvent[] events)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("aeg-sas-key", eventGridKey);
            HttpResponseMessage response = null;
            for (int i = 0; i < _maximumRetries; i++)
            {
                response = httpClient.PostAsJsonAsync(eventGridEndpoint, events).Result;
                // If the request was successful or the error is not a transient error, return
                if (response.IsSuccessStatusCode || !IsTransient(response.StatusCode))
                {
                    break;
                }
                Thread.Sleep(_retryDelay); // Delay then retry
            }

            return response;
        }

        private static bool IsTransient(System.Net.HttpStatusCode statusCode)
        {
            if (statusCode == System.Net.HttpStatusCode.GatewayTimeout
                || statusCode == System.Net.HttpStatusCode.RequestTimeout
                || statusCode == System.Net.HttpStatusCode.ServiceUnavailable
                || statusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
