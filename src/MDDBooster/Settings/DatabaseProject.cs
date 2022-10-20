using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
    public class DatabaseProject
    {
        [JsonConstructor]
        public DatabaseProject(string path)
        {
            Path = path;
        }

        public string Path { get; set; }
    }
}