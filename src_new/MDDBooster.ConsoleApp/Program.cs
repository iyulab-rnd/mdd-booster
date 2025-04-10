﻿using MDDBooster.ConsoleApp.Models;
using MDDBooster.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.CommandLine;
using System.Text.Json;
using MDDBooster.Builders.MsSql;
using MDDBooster.Models;
using System.Reflection;
using MDDBooster.Builders.ModelProject;

namespace MDDBooster.ConsoleApp;

internal class Program
{
#if DEBUG
    private const string DefaultSettingsPath = @"D:\data\ironhive-appservice\mdd\settings.json";
#endif

    static async Task<int> Main(string[] args)
    {
#if DEBUG
        args = new string[] { "--settings", DefaultSettingsPath };
#endif

        // Create root command
        var rootCommand = new RootCommand("MDDBooster Console Application - M3L Parser and SQL Generator");

        // Add settings file option
        var settingsOption = new Option<string>(
            name: "--settings",
            description: "Path to the settings JSON file",
            getDefaultValue: () => DefaultSettingsPath);

        // Do NOT add another version option as it's already built into System.CommandLine
        rootCommand.AddOption(settingsOption);

        // Set handler for main command - only use the settings option
        rootCommand.SetHandler((settingsPath) =>
        {
            try
            {
                // Display current directory for debugging
                Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

                // Ensure all assemblies are loaded to find builders
                LoadAllAssemblies();

                // Initialize BuilderManager to discover all available builders
                BuilderManager.Initialize();

                // Log available builders for debugging
                var availableBuilders = BuilderManager.GetAvailableBuilderTypes();
                Console.WriteLine($"Available builders: {string.Join(", ", availableBuilders)}");

                // Load application settings from the JSON file
                // This now correctly resolves all paths
                var settings = Settings.Load(settingsPath);

                // Initialize logging based on settings
                InitializeLogging(settings.Logging);

                // Log available builders
                Log.Information("Available builders: {Builders}",
                    string.Join(", ", BuilderManager.GetAvailableBuilderTypes()));

                // Process each MDD file configuration
                foreach (var mddConfig in settings.MddConfigs)
                {
                    ProcessMddConfig(mddConfig);
                }

                Console.WriteLine("Processing completed successfully.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
                Log.Error(ex, "Unhandled exception");
            }
        }, settingsOption);

        // Execute root command
        return await rootCommand.InvokeAsync(args);
    }

    /// <summary>
    /// Loads all assemblies in the application directory to ensure all builders are discovered
    /// </summary>
    private static void LoadAllAssemblies()
    {
        try
        {
            // Get the current application directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            Console.WriteLine($"Scanning for assemblies in: {baseDir}");

            // Find all DLL files in the directory
            var dllFiles = Directory.GetFiles(baseDir, "*.dll");
            Console.WriteLine($"Found {dllFiles.Length} assemblies");

            // Load each assembly
            foreach (var dllPath in dllFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(dllPath);
                    // Only load MDDBooster related assemblies
                    if (fileName.StartsWith("MDDBooster"))
                    {
                        Console.WriteLine($"Loading assembly: {fileName}");
                        Assembly.LoadFrom(dllPath);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading assembly {dllPath}: {ex.Message}");
                }
            }

            // Verify the MsSqlBuilder exists in loaded assemblies
            bool msSqlBuilderFound = false;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetTypes().Any(t => t.Name == "MsSqlBuilder"))
                {
                    Console.WriteLine($"Found MsSqlBuilder in assembly: {assembly.FullName}");
                    msSqlBuilderFound = true;
                    break;
                }
            }

            if (!msSqlBuilderFound)
            {
                Console.WriteLine("WARNING: MsSqlBuilder type not found in any loaded assembly!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading assemblies: {ex.Message}");
        }
    }

    private static void DisplayVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine($"MDDBooster version {version}");
    }

    private static void InitializeLogging(LoggingSettings loggingSettings)
    {
        // Set minimum log level based on verbose flag
        var minLevel = loggingSettings.Verbose ?
            Serilog.Events.LogEventLevel.Debug :
            Serilog.Events.LogEventLevel.Information;

        // Configure Serilog with console output
        var serilogConfig = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code
            );

        // Add file logging if specified
        if (!string.IsNullOrEmpty(loggingSettings.LogFilePath))
        {
            // Ensure log directory exists
            string? logDir = Path.GetDirectoryName(loggingSettings.LogFilePath);
            if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            // Add file sink
            serilogConfig.WriteTo.File(
                loggingSettings.LogFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day
            );
        }

        // Create Serilog logger
        var serilogLogger = serilogConfig.CreateLogger();

        // Create Microsoft.Extensions.Logging factory with Serilog provider
        var serviceCollection = new ServiceCollection();
        var loggerFactory = serviceCollection
            .AddLogging(builder => builder.AddSerilog(serilogLogger, dispose: true))
            .BuildServiceProvider()
            .GetRequiredService<ILoggerFactory>();

        // Initialize the logging system
        MDDBooster.Logging.LoggingManager.Initialize(loggerFactory);

        // Create a logger for the Program class
        var logger = loggerFactory.CreateLogger<Program>();

        // Log startup info
        logger.LogInformation("MDDBooster starting up");
        logger.LogDebug("Verbose logging enabled: {Verbose}", loggingSettings.Verbose);

        if (!string.IsNullOrEmpty(loggingSettings.LogFilePath))
        {
            logger.LogInformation("Logging to file: {LogFilePath}", loggingSettings.LogFilePath);
        }
    }

    private static void ProcessMddConfig(MddConfig mddConfig)
    {
        // Validate MDD file path
        if (string.IsNullOrEmpty(mddConfig.MddPath))
        {
            Console.WriteLine("Warning: Empty MDD file path in configuration. Skipping.");
            return;
        }

        if (!File.Exists(mddConfig.MddPath))
        {
            Console.WriteLine($"Warning: MDD file not found: {mddConfig.MddPath}. Skipping.");
            return;
        }

        Log.Information("Processing MDD file: {FilePath}", mddConfig.MddPath);

        try
        {
            // Parse the MDD file
            var document = ParseMddFile(mddConfig.MddPath);
            if (document == null)
                return;

            // Apply each configured builder
            foreach (var builderInfo in mddConfig.Builders)
            {
                ApplyBuilder(document, builderInfo);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing MDD file: {FilePath}", mddConfig.MddPath);
            Console.WriteLine($"Error processing MDD file: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }

    private static MDDDocument? ParseMddFile(string filePath)
    {
        Log.Information("Parsing MDD file: {FilePath}", filePath);

        try
        {
            // Create parser with SQL options and MODEL options
            var options = new MDDParserOptions()
                .UseMsSqlBuilder()
                .UseModelProjectBuilder();

            var parser = new MDDBoosterParser(options);

            // Parse the file
            Log.Debug("Parsing MDD file with MDDBoosterParser");
            var document = parser.Parse(filePath);

            // Display summary of the parsed document
            Log.Information("Successfully parsed file with namespace: {Namespace}", document.BaseDocument.Namespace);
            Console.WriteLine($"Namespace: {document.BaseDocument.Namespace}");
            Console.WriteLine($"Models: {document.Models.Count}");
            Console.WriteLine($"Interfaces: {document.Interfaces.Count}");
            Console.WriteLine($"Enums: {document.Enums.Count}");

            return document;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error parsing file: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Console.ResetColor();
            Log.Error(ex, "Error parsing file: {FilePath}", filePath);
            return null;
        }
    }

    private static void ApplyBuilder(MDDDocument document, BuilderInfo builderInfo)
    {
        Log.Information("Applying builder: {BuilderType}", builderInfo.Type);

        // Create builder instance
        var builder = BuilderManager.CreateBuilder(builderInfo.Type);
        if (builder == null)
        {
            Log.Error("Builder not found: {BuilderType}", builderInfo.Type);
            Console.WriteLine($"ERROR: Builder type '{builderInfo.Type}' not found. Available types: {string.Join(", ", BuilderManager.GetAvailableBuilderTypes())}");
            return;
        }

        try
        {
            // Convert builder config from JSON to the builder-specific config type
            var jsonElement = JsonSerializer.SerializeToElement(builderInfo.Config);
            var config = BuilderManager.ConvertConfig(builderInfo.Type, jsonElement);

            if (config == null)
            {
                Log.Error("Failed to create config for builder: {BuilderType}", builderInfo.Type);
                return;
            }

            // Ensure the project path exists
            if (config is IBuilderConfig builderConfig)
            {
                string projectPath = builderConfig.ProjectPath;
                if (!string.IsNullOrEmpty(projectPath) && !Directory.Exists(projectPath))
                {
                    Log.Warning("Project directory does not exist: {ProjectPath}. Attempting to create it.", projectPath);
                    try
                    {
                        Directory.CreateDirectory(projectPath);
                        Log.Information("Created project directory: {ProjectPath}", projectPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to create project directory: {ProjectPath}", projectPath);
                        Console.WriteLine($"ERROR: Failed to create project directory: {projectPath}. {ex.Message}");
                        return;
                    }
                }
            }

            // Process the document with the builder
            bool success = builder.Process(document, config);

            if (success)
            {
                Log.Information("Builder {BuilderType} completed successfully", builderInfo.Type);
            }
            else
            {
                Log.Error("Builder {BuilderType} failed to process document", builderInfo.Type);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error applying builder {BuilderType}", builderInfo.Type);
            Console.WriteLine($"Error applying builder {builderInfo.Type}: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }
    }
}