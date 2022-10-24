using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
    public class ServerProject
    {
        [JsonConstructor]
        public ServerProject(string path, string @namespace)
        {
            Path = path;
            Namespace = @namespace;
        }

        public string Path { get; set; }
        
        [JsonPropertyName("ns")]
        public string Namespace { get; set; }
    }
}