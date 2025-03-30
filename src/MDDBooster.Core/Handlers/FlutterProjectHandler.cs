using MDDBooster.Builders;
using MDDBooster.Models;
using MDDBooster.Settings;
using Microsoft.Extensions.Logging;

namespace MDDBooster.Handlers
{
    public class FlutterProjectHandler(ILogger<FlutterProjectHandler> logger, Settings.Settings settings)
    {
        private readonly ILogger<FlutterProjectHandler> logger = logger;
        private readonly Settings.Settings settings = settings;

        internal async Task RunAsync(IModelMeta[] models)
        {
            if (settings.FlutterProject == null) return;

            if (settings.FlutterProject.Output != null)
            {
                await BuildDtoModelsAsync(models, settings.FlutterProject.Output);
            }

            if (settings.FlutterProject.Models != null)
            {
                foreach (var m in settings.FlutterProject.Models)
                {
                    await BuildModelFileAsync(m);
                }
            }
        }

        private Task BuildDtoModelsAsync(IModelMeta[] models, string output)
        {
            logger.LogInformation($"Build FlutterProject DTO Models");
            var builder = new DartModelBuilder();

            var outputPath = Utils.ResolvePath(settings.BasePath, output);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
                Directory.CreateDirectory(outputPath);
            }
            else
            {
                Directory.CreateDirectory(outputPath);
            }
            return builder.BuildAsync(models, outputPath);
        }

        private Task BuildModelFileAsync(FlutterProjectModel m)
        {
            logger.LogInformation($"Build FlutterProject Model File");
            if (m.CsFile != null && m.DartFile != null)
            {
                var csFile = Utils.ResolvePath(settings.BasePath, m.CsFile);
                var dartFile = Utils.ResolvePath(settings.BasePath, m.DartFile);

                var builder = new DartModelBuilder();
                return builder.BuildAsync(csFile, dartFile);
            }
            else
            {
                return Task.CompletedTask;
            }
        }
    }
}