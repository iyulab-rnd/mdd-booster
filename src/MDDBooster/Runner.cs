using MDDBooster.Builders;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MDDBooster
{
    internal class Runner
    {
        private readonly ILogger<Runner> logger;
        private readonly Settings settings;

        public Runner(ILogger<Runner> logger, Settings settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

        internal async Task RunAsync()
        {
            var filePath = settings.GetTablesFilePath();
            if (System.IO.File.Exists(filePath) != true) throw new Exception($"cannot find tables file");

            var fileText = await File.ReadAllTextAsync(filePath);
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
                logger.LogInformation($"Build SQL: {m.Name}");

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
                logger.LogInformation($"Build Interface Class: {m.Name}");

                var builder = new InterfaceBuilder(m);
                builder.Build(ns, basePath);
            }

            foreach (var m in models.OfType<TableMeta>())
            {
                logger.LogInformation($"Build Entity Class: {m.Name}");

                var builder = new EntityBuilder(m);
                builder.Build(ns, basePath);
            }
        }
    }
}