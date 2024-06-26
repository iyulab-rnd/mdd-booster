using MDDBooster.Builders;
using MDDBooster.Settings;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MDDBooster.Handlers
{
    public class FlutterProjectHandler(ILogger<FlutterProjectHandler> logger, Settings.Settings settings)
    {
        private readonly ILogger<FlutterProjectHandler> logger = logger;
        private readonly Settings.Settings settings = settings;

        internal async Task RunAsync(IModelMeta[] _)
        {
            if (settings.FlutterProject == null) return;

            if (settings.FlutterProject.Models != null)
            {
                foreach (var m in settings.FlutterProject.Models)
                {
                    await BuildModelFileAsync(m);
                }
            }
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