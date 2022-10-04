using Azure.Storage.Blobs;
using AzureWebApiSolution.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using System.Drawing;
using System.Drawing.Imaging;

namespace AzureWebApiSolution.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class DemoController : ControllerBase
    {
        private readonly ILogger<DemoController> _logger;
        private readonly CosmosClient _cosmosClient;
        private readonly IConfiguration _configuration;
        private readonly BlobContainerClient _blobContainerClient;
        public DemoController(ILogger<DemoController> logger, IConfiguration config)
        {
            _logger = logger;
            _configuration = config;
            _blobContainerClient = new BlobContainerClient(_configuration["AzureWebJobsStorage"], "images");
            _cosmosClient = new CosmosClient(_configuration["CosmosConnection"]);
        }

        // Here we have the endpoint, where we take a string as an input and output it
        [HttpGet]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult GetString([FromQuery] string str)
        {
            _logger.LogInformation("Called http REST endpoint GetString from Swagger");
            return Ok($"Received {str} from http request. timestamp: {DateTime.Now}");
        }

        // Here we have the endpoint, where we take a two integers as an input and output it
        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public IActionResult GetInt([FromQuery] int nr1, int nr2)
        {
            _logger.LogInformation("Called http REST endpoint GetInt from Swagger");
            return Ok($"Received {nr1} & {nr2} from http request. {nr1} + {nr2} = {nr1 + nr2} Timestamp: {DateTime.Now}");
        }

        // Here we have the endpoint which resizes an image. Note that the logic is almost identical to the one from Day 1.
        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> ResizeImage(string fileName, IFormFile formFile)
        {
            try
            {
                var formcollection = await Request.ReadFormAsync();
                var file = formcollection.Files.FirstOrDefault();


                var image = Image.FromStream(file.OpenReadStream());
                var resized = new Bitmap(image, new Size(256, 256));

                using var imageStream = new MemoryStream();
                resized.Save(imageStream, ImageFormat.Jpeg);
                imageStream.Position = 0;

                var blob = _blobContainerClient.GetBlobClient(fileName + ".png");
                await blob.UploadAsync(imageStream);

                return Ok($"{file.FileName} - Uploaded as: {fileName}.png - size: {file.Length} bytes");
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        // Here we have the endpoint which uploads an image to our Storage Account in Azure - into the "images" container.
        [HttpPost]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public async Task<IActionResult> UploadImage(string fileName, IFormFile formFile)
        {
            try
            {
                var formcollection = await Request.ReadFormAsync();
                var file = formcollection.Files.FirstOrDefault();
              
                var blob = _blobContainerClient.GetBlobClient(fileName + ".png");
                await blob.UploadAsync(file.OpenReadStream());

                return Ok($"{file.FileName} - Uploaded as: {fileName}.png - size: {file.Length} bytes");
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

                });

                //Create new object in Cosmos from request
                var response = await containerLink.Container.UpsertItemAsync(metadata, new PartitionKey(metadata.BilledeId));
                
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

                });

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
