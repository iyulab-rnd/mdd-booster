using System.Reflection;
using System.Text.Json.Serialization;

namespace MDDBooster.Settings
{
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
        public WebFrontEnd? WebFrontEnd { get; set; }

        #endregion

        internal string? GetTablesFilePath() => Path.Combine(BasePath!, TableFileName);
    }
}