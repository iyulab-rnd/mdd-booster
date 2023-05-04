using MDDBooster.Builders;
using MDDBooster.Handlers;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MDDBooster
{
    internal partial class Runner
    {
        private readonly ILogger<Runner> logger;
        private readonly Settings.Settings settings;
        private readonly DatabaseProjectHandler databaseProjectHandler;
        private readonly ModelProjectHandler modelProjectHandler;
        private readonly ServerProjectHandler serverProjectHandler;
        private readonly WebFrontEndHandler webFrontEndHandler;
        private readonly string[] extensions = new string[] { ".mdd", ".m3l" };

        public Runner(ILogger<Runner> logger, Settings.Settings settings,
            DatabaseProjectHandler databaseProjectHandler,
            ModelProjectHandler modelProjectHandler,
            ServerProjectHandler serverProjectHandler,
            WebFrontEndHandler webFrontEndHandler)
        {
            this.logger = logger;
            this.settings = settings;
            this.databaseProjectHandler = databaseProjectHandler;
            this.modelProjectHandler = modelProjectHandler;
            this.serverProjectHandler = serverProjectHandler;
            this.webFrontEndHandler = webFrontEndHandler;
        }

        internal async Task RunAsync()
        {
            if (settings.BasePath == null) return;

            logger.LogInformation("running");

            foreach (var filePath in Directory.GetFiles(settings.BasePath))
            {
                var ext = Path.GetExtension(filePath).ToLower();
                if (extensions.Contains(ext) != true) continue;

                logger.LogInformation($"run: {Path.GetFileName(filePath)}");

                var fileText = await File.ReadAllTextAsync(filePath);
                var models = Parse(fileText);

                Resolver.Models = models;

                await databaseProjectHandler.RunAsync(models);
                await modelProjectHandler.RunAsync(models);
                await serverProjectHandler.RunAsync(models);
                await webFrontEndHandler.RunAsync(models);
            }

            logger.LogInformation("done.");
        }

        private static IModelMeta[] Parse(string text)
        {
            var blocks = new List<string>();
            var sb = new StringBuilder();
            foreach(var line in text.Split(Environment.NewLine))
            {
                if (line.StartsWith("##"))
                {
                    if (sb.Length > 0) blocks.Add(sb.ToString());

                    sb.Clear();
                    sb.AppendLine(line);
                }
                else if (line.StartsWith("-"))
                {
                    sb.AppendLine(line);
                }
            }
            if (sb.Length > 0) blocks.Add(sb.ToString());

            var models = new List<IModelMeta>();
            foreach (var block in blocks)
            {   
                var model = ModelMetaFactory.Create(block);
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