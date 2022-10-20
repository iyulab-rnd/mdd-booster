using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MDDBooster
{
    internal class App
    {
        private readonly ILogger<App> logger;
        private readonly Runner runner;

        public App(ILogger<App> logger, Runner runner)
        {
            this.logger = logger;
            this.runner = runner;
        }

        internal async Task RunAsync()
        {
            logger.LogInformation("running...");

            try
            {
                await runner.RunAsync();

                logger.LogInformation("done.");
            }
            catch (Exception e)
            {
                logger.LogError(e.Message + Environment.NewLine + e.StackTrace);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }
}