using Microsoft.Extensions.Logging;

namespace MDDBooster
{
    public class App
    {
        private readonly ILogger<App> logger;
        private readonly Runner runner;

        public App(ILogger<App> logger, Runner runner)
        {
            this.logger = logger;
            this.runner = runner;

            AppFunctions.Logger = logger;
        }

        public async Task RunAsync()
        {
            logger.LogInformation("running...");

#if DEBUG
            await runner.RunAsync();
            logger.LogInformation("done.");
#else
            try
            {
                await runner.RunAsync();

                logger.LogInformation("done.");
            }
            catch (Exception e)
            {
                logger.LogError($"{e.Message}{Constants.NewLine}{e.StackTrace}");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
#endif
        }
    }
}