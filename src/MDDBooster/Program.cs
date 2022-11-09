using MDDBooster;
using MDDBooster.Handlers;
using MDDBooster.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;

var stopwatch = Stopwatch.StartNew();

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
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
        var settings = JsonSerializer.Deserialize<Settings>(File.OpenRead(filePath), options);
        if (settings == null) throw new Exception("cannot read settings");
        settings.BasePath ??= Path.GetDirectoryName(filePath);

        Resolver.Settings = settings;

        services.AddSingleton(settings);
        services.AddSingleton<App>();
        services.AddSingleton<Runner>();

        services.AddSingleton<ModelProjectHandler>();
        services.AddSingleton<DatabaseProjectHandler>();
        services.AddSingleton<ServerProjectHandler>();
    })
    //.ConfigureLogging(config =>
    //{
    //    config.ClearProviders();
    //    config.AddSimpleConsole(p => p.SingleLine = true);
    //})
    .Build();

var app = host.Services.GetRequiredService<App>();
await app.RunAsync();

Console.WriteLine($"Code Generated Done. {stopwatch.Elapsed}");