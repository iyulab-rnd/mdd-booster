using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
    public class ModelProject
    {
        [JsonConstructor]
        public ModelProject(string path, string @namespace)
        {
            Path = path;
            Namespace = @namespace;
        }

        public string Path { get; set; }

        [JsonPropertyName("ns")]
        public string Namespace { get; set; }

        public string[]? Usings { get; set; }
    }
}