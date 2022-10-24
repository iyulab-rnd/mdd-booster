using MDDBooster.Builders;
using Microsoft.Extensions.Logging;

namespace MDDBooster.Handlers
{
    internal class ModelProjectHandler
    {
        private readonly ILogger<ModelProjectHandler> logger;
        private readonly Settings.Settings settings;

        private readonly string[] exceptNames = new string[] 
        { 
            "IEntity", "IIdEntity", "IKeyEntity", "IAtEntity", "IUndeletable" ,
            "IdEntity", "KeyEntity"
        };

        public ModelProjectHandler(ILogger<ModelProjectHandler> logger, Settings.Settings settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

        internal async Task RunAsync(IModelMeta[] models)
        {
            if (settings.ModelProject == null) return;

            var projPath = Utils.ResolvePath(settings.BasePath, settings.ModelProject.Path);
            if (projPath == null) return;

            var ns = settings.ModelProject.Namespace;
            if (ns == null) throw new Exception("required settings, model-ns");

            var basePath = Path.Combine(projPath, "Entity_");
            if (Directory.Exists(basePath)) Directory.Delete(basePath, true);
            Directory.CreateDirectory(basePath);

            foreach (var m in models.OfType<InterfaceMeta>())
            {
                if (exceptNames.Contains(m.Name)) continue;
                logger.LogInformation("Build interface class: {Name}", m.Name);

                var builder = new InterfaceBuilder(m);
                builder.Build(ns, basePath);
            }

            foreach (var m in models.OfType<AbstractMeta>())
            {
                if (exceptNames.Contains(m.Name)) continue;
                logger.LogInformation("Build abstract class: {Name}", m.Name);

                var builder = new EntityBuilder(m);
                builder.Build(ns, basePath);
            }

            foreach (var m in models.OfType<TableMeta>())
            {
                if (exceptNames.Contains(m.Name)) continue;
                logger.LogInformation("Build entity class: {Name}", m.Name);

                var builder = new EntityBuilder(m);
                builder.Build(ns, basePath);
            }

            await Task.CompletedTask;
        }
    }
}
