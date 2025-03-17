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
        public ModelProject ModelProject { get; set; }
        public DatabaseProject? DatabaseProject { get; set; }
        public ServerProject? ServerProject { get; set; }
        public WebFrontEnd? WebFrontEnd { get; set; }
        public FlutterProject? FlutterProject { get; set; }

        #endregion
    }
}