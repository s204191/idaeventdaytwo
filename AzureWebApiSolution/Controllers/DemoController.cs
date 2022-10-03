using Microsoft.AspNetCore.Mvc;
using System.Drawing.Imaging;
using Azure.Storage.Blobs;
using System.Drawing;
using Microsoft.Azure.Cosmos;
using AzureWebApiSolution.Models;

namespace AzureWebApiSolution.Controllers
{    
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class DemoController : ControllerBase
    {
        private readonly ILogger<DemoController> _logger;
        private readonly CosmosClient _cosmosClient;
        public DemoController(ILogger<DemoController> logger)
        {
            _logger = logger;
            //_cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosConnection"));
        }

        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult GetString([FromQuery] string str)
        {
            _logger.LogInformation("Called http REST endpoint GetString from Swagger");
            return Ok($"Received {str} from http request. timestamp: {DateTime.Now}");
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public IActionResult GetInt([FromQuery] int nr1, int nr2)
        {
            _logger.LogInformation("Called http REST endpoint GetInt from Swagger");
            return Ok($"Received {nr1} & {nr2} from http request. {nr1} + {nr2} = {nr1 + nr2} Timestamp: {DateTime.Now}");
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResizeImage(string fileName, IFormFile formFile)
        {
            try
            {
                //TODO: Fix mig
                //var formdata = await req.ReadFormAsync();
                //var file = req.Form.Files["file"];
                var formcollection = await Request.ReadFormAsync();
                var file = formcollection.Files.FirstOrDefault();


                var image = Image.FromStream(file.OpenReadStream());
                var resized = new Bitmap(image, new Size(256, 256));

                using var imageStream = new MemoryStream();
                resized.Save(imageStream, ImageFormat.Jpeg);
                imageStream.Position = 0;

                var blobService = new BlobServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                var blobClient = new BlobContainerClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "Images");
                var blob = blobClient.GetBlobClient(fileName);
                await blob.UploadAsync(imageStream);

                return Ok(file.FileName + " - " + file.Length.ToString() + " bytes");
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> SetMetaData(Metadata metadata)
        {
            try
            {
                //Get cosmos container
                var db = _cosmosClient.GetDatabase("MetaData");
                var containerLink = await db.CreateContainerIfNotExistsAsync(new ContainerProperties
                {
                    Id = "MyMetaData",
                    PartitionKeyPath = "/id",
                    DefaultTimeToLive = -1,

                }, ThroughputProperties.CreateAutoscaleThroughput(1000));

                //Create new object in Cosmos from request
                var response = await containerLink.Container.UpsertItemAsync(metadata, new PartitionKey(metadata.Id));
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(Metadata), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMetaData(string billedeId)
        {
            try
            {
                //Get cosmos container
                var db = _cosmosClient.GetDatabase("MetaData");
                var containerLink = await db.CreateContainerIfNotExistsAsync(new ContainerProperties
                {
                    Id = "MyMetaData",
                    PartitionKeyPath = "/id",
                    DefaultTimeToLive = -1,

                }, ThroughputProperties.CreateAutoscaleThroughput(1000));

                //Get specifiedId (if existing)
                var sqlQuery = $"SELECT * FROM c WHERE c.Id = '{billedeId}'"; // <-- Query to specify search

                var result = containerLink.Container.GetItemQueryIterator<Metadata>(sqlQuery);
                while (result.HasMoreResults)
                {
                    var message = await result.ReadNextAsync();
                    return Ok(message.FirstOrDefault());                
                }

                return Ok(billedeId + " Something went wrong!");
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
