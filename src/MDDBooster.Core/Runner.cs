using MDDBooster.Handlers;
using Microsoft.Extensions.Logging;
using System.Text;

namespace MDDBooster
{
    public partial class Runner(ILogger<Runner> logger, Settings.Settings settings,
        DatabaseProjectHandler databaseProjectHandler,
        ModelProjectHandler modelProjectHandler,
        ServerProjectHandler serverProjectHandler,
        WebFrontEndHandler webFrontEndHandler,
        FlutterProjectHandler flutterProjectHandler)
    {
        private readonly ILogger<Runner> logger = logger;
        private readonly Settings.Settings settings = settings;
        private readonly DatabaseProjectHandler databaseProjectHandler = databaseProjectHandler;
        private readonly ModelProjectHandler modelProjectHandler = modelProjectHandler;
        private readonly ServerProjectHandler serverProjectHandler = serverProjectHandler;
        private readonly WebFrontEndHandler webFrontEndHandler = webFrontEndHandler;
        private readonly FlutterProjectHandler flutterProjectHandler = flutterProjectHandler;

        private readonly string[] extensions = [".mdd", ".m3l"];

        public async Task RunAsync()
        {
            if (settings.BasePath == null) return;

            logger.LogInformation("running");
            try
            {

                foreach (var filePath in Directory.GetFiles(settings.BasePath))
                {
                    var ext = Path.GetExtension(filePath).ToLower();
                    if (extensions.Contains(ext) != true) continue;

                    logger.LogInformation("run: {filePath}", Path.GetFileName(filePath));

                    var fileText = await File.ReadAllTextAsync(filePath);
                    var models = MDDParser.Parse(fileText);

                    Resolver.Init(settings, models);

                    await databaseProjectHandler.RunAsync(models);
                    await modelProjectHandler.RunAsync(models);
                    await serverProjectHandler.RunAsync(models);
                    await webFrontEndHandler.RunAsync(models);
                    await flutterProjectHandler.RunAsync(models);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "{message}", e.Message);
                throw;
            }

            logger.LogInformation("done.");
        }
    }
}