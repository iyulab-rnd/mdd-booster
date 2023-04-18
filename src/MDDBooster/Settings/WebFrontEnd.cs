using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
    public class WebFrontEnd
    {
        public IEnumerable<WebFrontEndModel>? Models { get; set; }
    }

    public class WebFrontEndModel
    {
        [JsonPropertyName("ns")]
        public required string NS { get; set; }
        public required string ModelPath { get; set; }

        [JsonPropertyName("ts-file")]
        public required string TsFile { get; set; }
    }
}