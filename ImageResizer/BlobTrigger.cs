using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using static System.Net.WebRequestMethods;
using System.Threading.Tasks;

namespace ImageResizer
{
    public class BlobTrigger
    {
        [FunctionName("BlobTrigger")]
        public async Task RunAsync([BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
            if (!name.Contains("resized"))
            {
                var containerName = Environment.GetEnvironmentVariable("ContainerName");
                var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                try
                {
                    var image = Image.FromStream(myBlob);
                    var resized = new Bitmap(image, new Size(256, 256));

                    using var imageStream = new MemoryStream();
                    resized.Save(imageStream, ImageFormat.Jpeg);
                    imageStream.Position = 0;

                    var blobClient = new BlobContainerClient(conn, containerName);
                    var newName = $"resized-{name}";
                    var blob = blobClient.GetBlobClient(newName);
                    await blob.UploadAsync(imageStream);
                    log.LogInformation($"Blob trigger resixzed image and saved as: {newName}");
                }
                catch (Exception ex)
                {
                    log.LogError($"Error uploading to blob: {ex}");
                }
            }            
        }
    }
}
