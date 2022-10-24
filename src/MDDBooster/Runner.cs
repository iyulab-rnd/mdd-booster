using MDDBooster.Builders;
using MDDBooster.Handlers;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MDDBooster
{
    internal class Runner
    {
        private readonly ILogger<Runner> logger;
        private readonly Settings.Settings settings;
        private readonly DatabaseProjectHandler databaseProjectHandler;
        private readonly ModelProjectHandler modelProjectHandler;
        private readonly ServerProjectHandler serverProjectHandler;

        public Runner(ILogger<Runner> logger, Settings.Settings settings,
            DatabaseProjectHandler databaseProjectHandler,
            ModelProjectHandler modelProjectHandler,
            ServerProjectHandler serverProjectHandler)
        {
            this.logger = logger;
            this.settings = settings;
            this.databaseProjectHandler = databaseProjectHandler;
            this.modelProjectHandler = modelProjectHandler;
            this.serverProjectHandler = serverProjectHandler;
        }

        internal async Task RunAsync()
        {
            var filePath = settings.GetTablesFilePath();
            if (System.IO.File.Exists(filePath) != true) throw new Exception($"cannot find tables file");

            var fileText = await File.ReadAllTextAsync(filePath);
            var models = Parse(fileText);

            Resolver.Models = models;

            await databaseProjectHandler.RunAsync(models);
            await modelProjectHandler.RunAsync(models);
            await serverProjectHandler.RunAsync(models);
        }

        private static IModelMeta[] Parse(string text)
        {
            var maches = Regex.Matches(text, @"\#\s+(.*?)(\r\n\r\n|$)", RegexOptions.Singleline);

            var models = new List<IModelMeta>();

            foreach (var m in maches)
            {
                var content = m.ToString();
                if (string.IsNullOrEmpty(content)) continue;

                var model = ModelMetaFactory.Create(content);
                if (model == null) continue;

                models.Add(model);
            }

            models.OfType<ModelMetaBase>().ToList().ForEach(p =>
            {
                var inherits = p.GetInherits();
                
                var interfaces = new List<InterfaceMeta>();
                AbstractMeta? abstractMeta = null;

                foreach (var inheritName in inherits)
                {
                    var m = models.FirstOrDefault(p => p.Name == inheritName);
                    if (m == null)
                    {
                        p.AbstractName = inheritName;
                        continue;
                    }

                    if (m is InterfaceMeta interfaceMeta)
                        interfaces.Add(interfaceMeta);

                    else if (m is AbstractMeta mAbs)
                    {
                        if (abstractMeta != null) throw new Exception("두개이상의 추상클래스는 부여 할 수 없습니다.");

                        abstractMeta = mAbs;
                    }
                    else
                        throw new NotImplementedException();
                }

                p.Interfaces = interfaces.ToArray();
                p.Abstract = abstractMeta;
            });

            var defaults = models.OfType<ModelMetaBase>().FirstOrDefault(p => p.IsDefault());
            if (defaults != null)
            {
                foreach (var p in models.OfType<TableMeta>())
                {
                    if (defaults is InterfaceMeta m1)
                        p.Interfaces = new[] { m1 };

                    else if (defaults is AbstractMeta m2)
                        p.Abstract = m2;
                }
            }

            return models.ToArray();
        }
    }
}