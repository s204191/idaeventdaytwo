using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.EventHubs;
using System.Text;

namespace EventhubDemo
{
    public static class HttpTrigger
    {
        [FunctionName("HttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsEventHub");            

            // Create a producer client that you can use to send events to an event hub
            var producerClient = new EventHubProducerClient(connectionString, "cbreventhub");

            // Create a batch of events 
            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            var eventData = new EventDTO
            {
                From = "CBR",
                Subject = "Event til Ramtin",
                Message = "Hello Mister",
            };

            //Convert object to json string
            var json = System.Text.Json.JsonSerializer.Serialize(eventData);

            //Add string as bytes
            eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await producerClient.SendAsync(eventBatch);
                Console.WriteLine($"Published event from {eventData.From} with subject:  {eventData.Subject}.");
            }
            finally
            {
                await producerClient.DisposeAsync();
            }


            return new OkResult();
        }
    }
}
