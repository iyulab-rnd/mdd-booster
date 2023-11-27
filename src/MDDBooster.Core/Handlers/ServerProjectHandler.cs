using MDDBooster.Builders;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MDDBooster.Handlers
{
    public class ServerProjectHandler(ILogger<ServerProjectHandler> logger, Settings.Settings settings)
    {
        private readonly ILogger<ServerProjectHandler> logger = logger;
        private readonly Settings.Settings settings = settings;

        internal async Task RunAsync(IModelMeta[] models)
        {
            if (settings.ServerProject == null) return;

            var projPath = Utils.ResolvePath(settings.BasePath, settings.ServerProject.Path);
            if (projPath == null) return;

            var basePath = Path.Combine(projPath, "Services_");
            if (Directory.Exists(basePath)) Directory.Delete(basePath, true);
            Directory.CreateDirectory(basePath);

            BuildDataContext(models, 
                settings.ModelProject.Namespace,
                settings.ServerProject.Namespace,
                basePath);

            BuildEntitySet(models,
                settings.ModelProject.Namespace,
                settings.ServerProject.Namespace,
                basePath);

            BuildGraphQL(models, settings);

            await Task.CompletedTask;
        }

        private void BuildDataContext(IModelMeta[] models, string modelNS, string serverNS, string basePath)
        {
            logger.LogInformation("Build DataContext");
            var builder = new DataContextBuilder(models);
            builder.Build(modelNS, serverNS, basePath);
        }

        private void BuildEntitySet(IModelMeta[] models, string modelNS, string serverNS, string basePath)
        {
            logger.LogInformation("Build EntitySet");
            var builder = new EntitySetBuilder(models);
            builder.Build(modelNS, serverNS, basePath);
        }

        private void BuildGraphQL(IModelMeta[] models, Settings.Settings settings)
        {
            logger.LogInformation("Build GraphQL");
            var builder = new GraphQLBuilder(models, settings);
            builder.Build();
        }
    }
}
