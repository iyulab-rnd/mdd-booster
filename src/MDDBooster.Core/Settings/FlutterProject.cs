using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
    public class FlutterProject
    {
        public IEnumerable<FlutterProjectModel>? Models { get; set; }
    }

    public class FlutterProjectModel
    {
        [JsonPropertyName("cs-file")] public string? CsFile { get; set; }
        [JsonPropertyName("dart-file")] public string? DartFile { get; set; }
    }
}