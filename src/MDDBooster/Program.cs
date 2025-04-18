﻿using MDDBooster;
using MDDBooster.Handlers;
using MDDBooster.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var stopwatch = Stopwatch.StartNew();

#if DEBUG
        //args = [@"D:\data\Plands\mdd"];
        //args = [@"D:\data\U-Platform\mdd"];
        //args = [@"D:\data\MLoop.AppService\mdd"];
        args = [@"D:\data\new-connect\mdd"];
#endif
        if (args.Length < 1) args = [Environment.CurrentDirectory];

        var filePath = Path.Combine(args[0], "settings.json");
        if (File.Exists(filePath) != true)
        {
            Console.WriteLine($"cannot find file - {filePath}");
            return;
        }

        using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
#pragma warning disable CA1869
                JsonSerializerOptions options = new()
                {
                    AllowTrailingCommas = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true,
                };
#pragma warning restore CA1869
                options.Converters.Add(new JsonStringEnumConverter());
                var settings = JsonSerializer.Deserialize<Settings>(File.OpenRead(filePath), options) ?? throw new Exception("cannot read settings");
                settings.BasePath ??= Path.GetDirectoryName(filePath);

                services.AddSingleton(settings);
                services.AddSingleton<App>();
                services.AddSingleton<Runner>();

                services.AddSingleton<ModelProjectHandler>();
                services.AddSingleton<DatabaseProjectHandler>();
                services.AddSingleton<ServerProjectHandler>();
                services.AddSingleton<WebFrontEndHandler>();
                services.AddSingleton<FlutterProjectHandler>();
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
    }
}