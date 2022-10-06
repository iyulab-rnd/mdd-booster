using MDDBooster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

#if DEBUG
args = new string[] { @"D:/data/Plands/Plands.Core/data" };
#endif

if (args.Length < 1) args = new string[] { Environment.CurrentDirectory };

var filePath = Path.Combine(args[0], "settings.json");
if (File.Exists(filePath) != true)
{
    Console.WriteLine($"cannot find file - {filePath}");
    return;
}

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        var options = new JsonSerializerOptions()
        {
            AllowTrailingCommas = true
        };
        var settings = JsonSerializer.Deserialize<Settings>(File.OpenRead(filePath), options);
        if (settings == null) throw new Exception("cannot read settings");
        settings.BasePath = Path.GetDirectoryName(filePath);

        services.AddSingleton(settings);
        services.AddSingleton<App>();
        services.AddSingleton<Runner>();
    })
    .ConfigureLogging(config =>
    {
        config.ClearProviders();
        config.AddSimpleConsole(p => p.SingleLine = true);
    })
    .Build();

var app = host.Services.GetRequiredService<App>();
await app.RunAsync();