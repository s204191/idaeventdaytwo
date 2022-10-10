using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace EventhubDemo
{
    public static class EventhubTrigger
    {
        [FunctionName("ReceiveEventhubevent")]
        public static async Task Run([EventHubTrigger("cbreventhub", Connection = "AzureWebJobsEventHub", ConsumerGroup = "cbr"),] string myEventHubMessage, ILogger log)
        {
            log.LogInformation($"C# function triggered to process a message: {myEventHubMessage}");

            //Convert object to json string
            var json = System.Text.Json.JsonSerializer.Deserialize<EventDTO>(myEventHubMessage);
            Console.WriteLine($"Received event from {json.From} with subject:    {json.Subject}.");
        }
    }
}