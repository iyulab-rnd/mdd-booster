using MDDBooster.Builders;
using MDDBooster.Settings;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MDDBooster.Handlers
{
    public class WebFrontEndHandler(ILogger<WebFrontEndHandler> logger, Settings.Settings settings)
    {
        private readonly ILogger<WebFrontEndHandler> logger = logger;
        private readonly Settings.Settings settings = settings;

        internal async Task RunAsync(IModelMeta[] _)
        {
            if (settings.WebFrontEnd == null) return;

            if (settings.WebFrontEnd.Models != null)
            {
                foreach (var m in settings.WebFrontEnd.Models)
                {
                    await BuildModelFileAsync(m);
                }
            }
        }

        private Task BuildModelFileAsync(WebFrontEndModel m)
        {
            logger.LogInformation($"Build WebFrontEnd Model File");
            var modelPath = Utils.ResolvePath(settings.BasePath, m.ModelPath);
            var tsFile = Utils.ResolvePath(settings.BasePath, m.TsFile);

            var builder = new TsModelBuilder();
            return builder.BuildAsync(m.NS, modelPath, tsFile);
        }
    }
}