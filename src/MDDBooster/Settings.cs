using System.Reflection;
using System.Text.Json.Serialization;

namespace MDDBooster
{
    public class Settings
    {
        public string? BasePath { get; set; }

        [JsonPropertyName("database-project")]
        public string? DatabaseProjectPath { get; set; }

        [JsonPropertyName("model-project")]
        public string? ModelProjectPath { get; set; }

        [JsonPropertyName("model-ns")]
        public string? ModelNS { get; set; }

        internal string? GetDatabaseProjectPath() => ResolveDir(DatabaseProjectPath);
        internal string? GetModelProjectPath() => ResolveDir(ModelProjectPath);

        private string? ResolveDir(string? parameterPath)
        {
            if (parameterPath == null) return null;
            if (System.IO.Path.IsPathRooted(parameterPath)) return parameterPath;

            var basePath = BasePath ?? Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
            var cd = new DirectoryInfo(basePath);
            var splits = parameterPath.Replace(@"\", "/").Split("/");

            var paths = new List<string>();
            foreach (var name in splits)
            {
                if (name.Equals(".."))
                    cd = cd!.Parent;

                else if (name.Equals("."))
                    continue;

                else
                    paths.Add(name);
            }

            paths.Insert(0, cd.ToString());
            var dir = new DirectoryInfo(Path.Combine(paths.ToArray()));

            return dir.ToString();
        }
    }
}