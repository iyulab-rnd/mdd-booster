using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MDDBooster
{
    internal class App
    {
        public static App Current { get; private set; }

        private readonly ILogger<App> logger;
        private readonly Runner runner;


        public App(ILogger<App> logger, Runner runner)
        {
            this.logger = logger;
            this.runner = runner;

            App.Current = this;
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
                logger.LogError($"{e.Message}{Environment.NewLine}{e.StackTrace}");
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        internal void WriteFile(string path, string code)
        {
            logger.LogInformation($"Write File: {Path.GetFileName(path)}");

            var text = code.Replace("\t", "    ");
            File.WriteAllText(path, text);
        }
    }
}