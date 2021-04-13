using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MultipleFileProcessorOrchestrator
{
    public static class FileProcessor
    {
        [FunctionName("FileProcessor_ProcessCSV")]
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

        [FunctionName("FileProcessor_ProcessPDF")]
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
    }
}
