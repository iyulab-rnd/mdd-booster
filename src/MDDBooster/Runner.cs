using MDDBooster.Builders;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MDDBooster
{
    internal class Runner
    {
        private readonly string fileText;
        private readonly Settings settings;

        internal Runner(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists != true) throw new Exception($"cannot find file - {filePath}");

            var basePath = fileInfo.DirectoryName!;
            var settingsPath = Path.Combine(basePath, "settings.json");
            if (File.Exists(settingsPath) != true) throw new Exception($"cannot find file - settings.json");

            var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath));
            if (settings == null) throw new Exception($"wrong file - settings.json");
            this.settings = settings;

            settings.BasePath = basePath;

            this.fileText = File.ReadAllText(filePath);
        }

        internal void Run()
        {
            var models = Parse(fileText);

            BuildSqlFiles(models);
            BuildModelFiles(models);
        }

        private static IModelMeta[] Parse(string text)
        {
            var maches = Regex.Matches(text, @"\#\s+(.*?)(\r\n\r\n|$)", RegexOptions.Singleline);

            var models = new List<IModelMeta>();

            foreach(var m in maches)
            {
                var content = m.ToString();
                if (string.IsNullOrEmpty(content)) continue;

                var model = ModelMetaFactory.Create(content);
                if (model == null) continue;

                models.Add(model);
            }

            models.OfType<ModelMetaBase>().ToList().ForEach(p =>
            {
                var interfaceNames = p.GetInterfaceNames();
                var list = new List<InterfaceMeta>();
                foreach(var interfaceName in interfaceNames)
                {
                    var m = models.OfType<InterfaceMeta>().First(p => p.Name == interfaceName);
                    list.Add(m);
                }
                p.Interfaces = list.ToArray();
            });

            var defaultInterface = models.OfType<InterfaceMeta>().FirstOrDefault(p => p.IsDefault());
            if (defaultInterface != null)
            {
                foreach (var p in models.OfType<TableMeta>().Where(p => p.Interfaces == null || p.Interfaces.Any() != true))
                {
                    p.Interfaces = new[] { defaultInterface };
                }
            }

            return models.ToArray();
        }


        private void BuildSqlFiles(IModelMeta[] models)
        {
            var projPath = settings.GetDatabaseProjectPath();
            if (projPath == null) return;

            var basePath = Path.Combine(projPath, "dbo", "Tables_");
            if (Directory.Exists(basePath)) Directory.Delete(basePath, true);
            Directory.CreateDirectory(basePath);

            foreach (var m in models.OfType<TableMeta>())
            {
                var builder = new SqlBuilder(m);
                builder.Build(basePath);
            }
        }

        private void BuildModelFiles(IModelMeta[] models)
        {
            var projPath = settings.GetModelProjectPath();
            if (projPath == null) return;

            var ns = settings.ModelNS;
            if (ns == null) throw new Exception("required settings, model-ns");

            var basePath = Path.Combine(projPath, "Data", "Entity_");
            if(Directory.Exists(basePath)) Directory.Delete(basePath, true);
            Directory.CreateDirectory(basePath);

            foreach (var m in models.OfType<InterfaceMeta>())
            {
                var builder = new InterfaceBuilder(m);
                builder.Build(ns, basePath);
            }

            foreach (var m in models.OfType<TableMeta>())
            {
                var builder = new EntityBuilder(m);
                builder.Build(ns, basePath);
            }
        }
    }
}