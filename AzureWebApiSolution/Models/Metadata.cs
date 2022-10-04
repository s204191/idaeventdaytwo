using System.Text.Json.Serialization;

namespace AzureWebApiSolution.Models
{
    public class Metadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        [JsonPropertyName("id")]
        public string BilledeId { get; set; }
        public string Location { get; set; }
    }
}
