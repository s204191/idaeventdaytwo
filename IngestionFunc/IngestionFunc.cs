using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AzureWebApiSolution.Models;
using Azure.Storage.Blobs;
using System.Drawing;
using System.Drawing.Imaging;

namespace IngestionFunc
{
    public static class IngestionFunc
    {
        [FunctionName("IngestionFunc")]
        public static async Task Run([EventHubTrigger("cbreventhub", Connection = "AzureWebJobsEventHub", ConsumerGroup = "cbr"),] string myEventHubMessage, ILogger log)
        {
            log.LogInformation($"C# function triggered to process a message: {myEventHubMessage}");

            //Convert object to json string
            var json = System.Text.Json.JsonSerializer.Deserialize<blobEvent>(myEventHubMessage);

            //Read from blob and resize
            var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            try
            {
                var blobClient = new BlobContainerClient(conn, "pics");
                var blob = blobClient.GetBlobClient(json.url);

                var responseStream = await blob.OpenReadAsync();               

                var image = Image.FromStream(responseStream);
                var resized = new Bitmap(image, new Size(256, 256));

                using var imageStream = new MemoryStream();
                resized.Save(imageStream, ImageFormat.Jpeg);
                imageStream.Position = 0;

                var newName = $"resized-{json.url}";
                var blobupload = blobClient.GetBlobClient(newName);
                await blobupload.UploadAsync(imageStream);
                log.LogInformation($"Blob trigger resixzed image and saved as: {newName}");
            }
            catch (Exception ex)
            {
                log.LogError($"Error uploading to blob: {ex}");
            }
        }
    }
}
