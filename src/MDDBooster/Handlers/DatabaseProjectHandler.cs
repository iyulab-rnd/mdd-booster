using MDDBooster.Builders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDDBooster.Handlers
{
    internal class DatabaseProjectHandler
    {
        private readonly ILogger<DatabaseProjectHandler> logger;
        private readonly Settings.Settings settings;

        public DatabaseProjectHandler(ILogger<DatabaseProjectHandler> logger, Settings.Settings settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

        internal async Task RunAsync(IModelMeta[] models)
        {
            if (settings.DatabaseProject == null) return;

            var projPath = Utils.ResolvePath(settings.BasePath, settings.DatabaseProject.Path);
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

            await Task.CompletedTask;
        }
    }
}
