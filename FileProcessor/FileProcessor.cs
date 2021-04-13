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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace FileProcessor
{
    public static class FileProcessor
    {
        [FunctionName("FileProcessor_ProcessCSV")]
        public static async Task<IActionResult> RunCSV(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            DistributorFile distributorFile = JsonConvert.DeserializeObject<DistributorFile>(requestBody);
            log.LogInformation($"Processing CSV file {distributorFile.FilePath}");
            JSONMessage jsonMessage = new JSONMessage()
            {
                OriginalFilePath = distributorFile.FilePath,
                FileName = Path.GetFileName(distributorFile.FilePath),
                FileLength = distributorFile.FileContents.Length
            };

            return new OkObjectResult(JsonConvert.SerializeObject(jsonMessage));
        }

        [FunctionName("FileProcessor_ProcessPDF")]
        public static async Task<IActionResult> RunPDF(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            DistributorFile distributorFile = JsonConvert.DeserializeObject<DistributorFile>(requestBody);
            log.LogInformation($"Processing PDF file {distributorFile.FilePath}");
            JSONMessage jsonMessage = new JSONMessage()
            {
                OriginalFilePath = distributorFile.FilePath,
                FileName = Path.GetFileName(distributorFile.FilePath),
                FileLength = distributorFile.FileContents.Length
            };

            return new OkObjectResult(JsonConvert.SerializeObject(jsonMessage));
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
