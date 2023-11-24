using MDDBooster.Builders;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDDBooster.Handlers
{
    public class DatabaseProjectHandler(ILogger<DatabaseProjectHandler> logger, Settings.Settings settings)
    {
        private readonly ILogger<DatabaseProjectHandler> logger = logger;
        private readonly Settings.Settings settings = settings;

        internal async Task RunAsync(IModelMeta[] models)
        {
            if (settings.DatabaseProject == null) return;

            var projPath = Utils.ResolvePath(settings.BasePath, settings.DatabaseProject.Path);
            if (projPath == null) return;

            var tablesPath = Path.Combine(projPath);
            if (settings.DatabaseProject.Kind == Settings.DatabaseKinds.MSSQL)
            {
                tablesPath = Path.Combine(tablesPath, "dbo", "Tables_");
            }
            else if (settings.DatabaseProject.Kind == Settings.DatabaseKinds.PostgreSQL)
            {
                tablesPath = Path.Combine(tablesPath, $"{settings.DatabaseProject.Kind}", "Tables_");
            }
            else
                throw new NotImplementedException();

            if (Directory.Exists(tablesPath)) Directory.Delete(tablesPath, true);
            Directory.CreateDirectory(tablesPath);

            //var triggersPath = Path.Combine(projPath, "dbo", "Triggers_");
            //if (Directory.Exists(triggersPath)) Directory.Delete(triggersPath, true);
            //Directory.CreateDirectory(triggersPath);

            foreach (var m in models.OfType<TableMeta>())
            {
                logger.LogInformation("Build SQL: {name}", m.Name);

                if (settings.DatabaseProject.Kind == Settings.DatabaseKinds.MSSQL)
                {
                    var builder = new SqlBuilder(m);
                    builder.Build(tablesPath);
                }
                else if (settings.DatabaseProject.Kind == Settings.DatabaseKinds.PostgreSQL)
                {
                    var builder = new PostgreSqlBuilder(m);
                    builder.Build(tablesPath);
                }
                else
                    throw new NotImplementedException();

                //var triggerBuilder = new SqlTriggerBuilder(m);
                //triggerBuilder.Build(triggersPath);
            }

            await Task.CompletedTask;
        }
    }
}
