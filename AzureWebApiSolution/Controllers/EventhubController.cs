using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using AzureWebApiSolution.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AzureWebApiSolution.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EventhubController : ControllerBase
    {
        private readonly ILogger<EventhubController> _logger;
        private readonly IConfiguration _configuration;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly EventHubProducerClient _eventHubProducerClient;

        public EventhubController(ILogger<EventhubController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _blobContainerClient = new BlobContainerClient(_configuration["RamtinsStorage"],"pics");
            _eventHubProducerClient = new EventHubProducerClient(_configuration["AzureWebJobsEventHub"], "cbreventhub");
        }

        // Here we have the endpoint which uploads an image to our Storage Account in Azure - into the "pics" container.
        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadImage(string fileName, IFormFile formFile)
        {
            _logger.LogInformation("Called http REST endpoint UploadImage from Swagger");
            try
            {
                var formcollection = await Request.ReadFormAsync();
                var file = formcollection.Files.FirstOrDefault();

                var blob = _blobContainerClient.GetBlobClient(fileName + ".png");
                await blob.UploadAsync(file.OpenReadStream());
                
                // Create a batch of events 
                using EventDataBatch eventBatch = await _eventHubProducerClient.CreateBatchAsync();

                var eventData = new blobEvent 
                { 
                    from = "insertName", 
                    name = fileName, 
                    url = fileName+".png"
                };

                //Convert object to json string
                var json = System.Text.Json.JsonSerializer.Serialize(eventData);

                //Add string as bytes
                eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));

                try
                {
                    // Use the producer client to send the batch of events to the event hub
                    await _eventHubProducerClient.SendAsync(eventBatch);
                    Console.WriteLine("Published event on eventhub");
                }
                finally
                {
                    await _eventHubProducerClient.DisposeAsync();
                }

                return Ok($"{file.FileName} - Uploaded as: {fileName}.png - size: {file.Length} bytes and published to EventHub");
            }
            catch (Exception ex)
            {

                return BadRequest($"error: {ex.Message}");
            }
        }
    }
}
