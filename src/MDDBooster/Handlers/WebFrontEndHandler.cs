using MDDBooster.Builders;
using MDDBooster.Settings;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MDDBooster.Handlers
{
    internal class WebFrontEndHandler
    {
        private readonly ILogger<WebFrontEndHandler> logger;
        private readonly Settings.Settings settings;

        public WebFrontEndHandler(ILogger<WebFrontEndHandler> logger, Settings.Settings settings)
        {
            this.logger = logger;
            this.settings = settings;
        }

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