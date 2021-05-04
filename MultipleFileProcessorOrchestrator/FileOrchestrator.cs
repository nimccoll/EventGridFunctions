using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MultipleFileProcessorOrchestrator
{
    public static class FileOrchestrator
    {
        private static TopicClient _topicClient;
        private static readonly string _topicName = "eventgriddemo";

        static FileOrchestrator()
        {
            // For performance reasons we are using a static instance of the TopicClient per best practices
            if (_topicClient == null)
            {
                string connectionString = Environment.GetEnvironmentVariable("ServiceBusConnString", EnvironmentVariableTarget.Process);
                // Set the retry policy
                var retryPolicy = new RetryExponential(TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(1000), 5);
                _topicClient = new TopicClient(connectionString, _topicName, retryPolicy);
            }
        }

        [FunctionName("FileOrchestrator")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            DistributorFile distributorFile = context.GetInput<DistributorFile>();
            log.LogInformation($"Running orchestration on file {distributorFile.FilePath}");
            string json = string.Empty;

            if (Path.GetExtension(distributorFile.FilePath) == ".csv")
            {
                json = await context.CallActivityAsync<string>("FileProcessor_ProcessCSV", distributorFile);
                //DurableHttpResponse durableHttpResponse = await context.CallHttpAsync(HttpMethod.Post, new Uri("http://localhost:7072/api/FileProcessor_ProcessCSV"), JsonConvert.SerializeObject(distributorFile));
                //if (durableHttpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    json = durableHttpResponse.Content;
                //}
            }
            else if (Path.GetExtension(distributorFile.FilePath) == ".pdf")
            {
                json = await context.CallActivityAsync<string>("FileProcessor_ProcessPDF", distributorFile);
                //DurableHttpResponse durableHttpResponse = await context.CallHttpAsync(HttpMethod.Post, new Uri("http://localhost:7072/api/FileProcessor_ProcessPDF"), JsonConvert.SerializeObject(distributorFile));
                //if (durableHttpResponse.StatusCode == System.Net.HttpStatusCode.OK)
                //{
                //    json = durableHttpResponse.Content;
                //}
            }

            if (!string.IsNullOrEmpty(json))
            {
                // Write JSON to Service Bus
                Message message = new Message(Encoding.UTF8.GetBytes(json));
                message.UserProperties["FileType"] = Path.GetExtension(distributorFile.FilePath);
                try
                {
                    await _topicClient.SendAsync(message);
                }
                catch (ServiceBusException ex)
                {
                    // If we reach this code, the retry policy was unable to handle the exception
                    // Log the exception and re-throw
                    log.LogError(ex.Message);
                    throw;
                }
           }

            return json;
        }

        [FunctionName("FileOrchestrator_ProcessCSV")]
        public static string ProcessCSV([ActivityTrigger] DistributorFile distributorFile, ILogger log)
        {
            log.LogInformation($"Processing CSV file {distributorFile.FilePath}");
            JSONMessage jsonMessage = new JSONMessage()
            {
                OriginalFilePath = distributorFile.FilePath,
                FileName = Path.GetFileName(distributorFile.FilePath),
                FileLength = distributorFile.FileContents.Length
            };

            return JsonConvert.SerializeObject(jsonMessage);
        }

        [FunctionName("FileOrchestrator_ProcessPDF")]
        public static string ProcessPDF([ActivityTrigger] DistributorFile distributorFile, ILogger log)
        {
            log.LogInformation($"Processing PDF file {distributorFile.FilePath}");
            JSONMessage jsonMessage = new JSONMessage()
            {
                OriginalFilePath = distributorFile.FilePath,
                FileName = Path.GetFileName(distributorFile.FilePath),
                FileLength = distributorFile.FileContents.Length
            };
            
            return JsonConvert.SerializeObject(jsonMessage);
        }

        [FunctionName("FileOrchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("FileOrchestrator_BlobStart")]
        public static async Task BlobStart([BlobTrigger("sftpupload/{name}", Connection = "BlobStorageConnString")] Stream myBlob, string name,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            string blobContents = string.Empty;
            using (StreamReader reader = new StreamReader(myBlob))
            {
                blobContents = reader.ReadToEnd();
            }
            DistributorFile distributorFile = new DistributorFile()
            {
                FilePath = name,
                FileContents = blobContents
            };
            string instanceId = await starter.StartNewAsync<DistributorFile>("FileOrchestrator", distributorFile);
        }
    }

    public class DistributorFile
    {
        public string FilePath { get; set; }
        public string FileContents { get; set; }
    }

    public class JSONMessage
    {
        public string OriginalFilePath { get; set; }
        public string FileName { get; set; }
        public int FileLength { get; set; }
    }
}