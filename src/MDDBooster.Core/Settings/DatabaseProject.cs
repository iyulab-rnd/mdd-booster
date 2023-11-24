using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
    public enum DatabaseKinds
    {
        MSSQL,
        PostgreSQL
    }

    public class DatabaseProject
    {
        [JsonConstructor]
        public DatabaseProject(string path)
        {
            Path = path;
        }

        public DatabaseKinds Kind { get; set; }
        public string Path { get; set; }
    }
}