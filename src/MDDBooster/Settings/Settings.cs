using System.Reflection;
using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
    //public class Settings
    //{
    //    public string? BasePath { get; set; }

    //    [JsonPropertyName("database-project")]
    //    public string? DatabaseProjectPath { get; set; }

    //    [JsonPropertyName("model-project")]
    //    public string? ModelProjectPath { get; set; }

    //    [JsonPropertyName("model-ns")]
    //    public string? ModelNS { get; set; }

    //    [JsonPropertyName("tables-path")]
    //    public string TablesPath { get; set; } = "tables.mdd";

    //    internal string? GetDatabaseProjectPath() => ResolveDir(DatabaseProjectPath);
    //    internal string? GetModelProjectPath() => ResolveDir(ModelProjectPath);
    //    internal string? GetTablesFilePath() => ResolveDir(TablesPath);
    //}


    public class Settings
    {
        [JsonConstructor]
        public Settings(ModelProject modelProject)
        {
            ModelProject = modelProject;
        }

        #region properties...

        public string? BasePath { get; set; }
        public string TableFileName { get; set; } = "tables.mdd";
        public ModelProject ModelProject { get; set; }
        public DatabaseProject? DatabaseProject { get; set; }
        public ServerProject? ServerProject { get; set; }

        #endregion

        internal string? GetTablesFilePath() => Path.Combine(BasePath!, TableFileName);
    }
}