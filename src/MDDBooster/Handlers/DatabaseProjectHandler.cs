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

            var tablesPath = Path.Combine(projPath, "dbo", "Tables_");
            if (Directory.Exists(tablesPath)) Directory.Delete(tablesPath, true);
            Directory.CreateDirectory(tablesPath);

            //var triggersPath = Path.Combine(projPath, "dbo", "Triggers_");
            //if (Directory.Exists(triggersPath)) Directory.Delete(triggersPath, true);
            //Directory.CreateDirectory(triggersPath);

            foreach (var m in models.OfType<TableMeta>())
            {
                logger.LogInformation($"Build SQL: {m.Name}");

                var builder = new SqlBuilder(m);
                builder.Build(tablesPath);

                //var triggerBuilder = new SqlTriggerBuilder(m);
                //triggerBuilder.Build(triggersPath);
            }

            await Task.CompletedTask;
        }
    }
}
