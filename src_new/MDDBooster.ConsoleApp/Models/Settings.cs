using System.Text.Json;
using System.Text.Json.Serialization;

namespace MDDBooster.ConsoleApp.Models;

/// <summary>
/// Application settings loaded from settings.json
/// </summary>
public class Settings
{
    /// <summary>
    /// List of MDD file configurations to process
    /// </summary>
    public List<MddConfig> MddConfigs { get; set; } = new List<MddConfig>();

    /// <summary>
    /// Logging settings
    /// </summary>
    public LoggingSettings Logging { get; set; } = new LoggingSettings();

    /// <summary>
    /// Path to the settings file (used for resolving relative paths)
    /// </summary>
    [JsonIgnore]
    public string SettingsFilePath { get; private set; }

    /// <summary>
    /// Load settings from a JSON file
    /// </summary>
    public static Settings Load(string filePath)
    {
        // Convert filePath to absolute path if it's relative
        string absoluteFilePath = Path.GetFullPath(filePath);

        if (!File.Exists(absoluteFilePath))
        {
            Console.WriteLine($"Settings file not found at: {absoluteFilePath}");
            var defaultSettings = CreateDefaultSettings();
            defaultSettings.SettingsFilePath = absoluteFilePath;
            SaveSettings(absoluteFilePath, defaultSettings);
            return defaultSettings;
        }

        string json = File.ReadAllText(absoluteFilePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(json, options);

            // Set settings file path for relative path resolution
            settings.SettingsFilePath = absoluteFilePath;

            // Ensure we have at least one configuration if deserialization succeeded
            if (settings != null)
            {
                if (settings.MddConfigs.Count == 0)
                {
                    settings.MddConfigs.Add(CreateDefaultMddConfig());
                }
                else
                {
                    // Apply defaults to each builder config
                    foreach (var mddConfig in settings.MddConfigs)
                    {
                        foreach (var builderInfo in mddConfig.Builders)
                        {
                            ApplyDefaultsToBuilderConfig(builderInfo);
                        }
                    }

                    // Resolve relative paths in MDD configurations
                    ResolvePaths(settings);
                }

                return settings;
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing settings file: {ex.Message}");
            Console.WriteLine("Using default settings instead.");
        }

        var defaultSettings2 = CreateDefaultSettings();
        defaultSettings2.SettingsFilePath = absoluteFilePath;
        return defaultSettings2;
    }

    /// <summary>
    /// Resolves relative paths in configuration to absolute paths
    /// </summary>
    private static void ResolvePaths(Settings settings)
    {
        string settingsDirectory = Path.GetDirectoryName(settings.SettingsFilePath);

        foreach (var mddConfig in settings.MddConfigs)
        {
            // Resolve MDD file path if it's relative
            if (!string.IsNullOrEmpty(mddConfig.MddPath) && !Path.IsPathRooted(mddConfig.MddPath))
            {
                mddConfig.MddPath = Path.GetFullPath(Path.Combine(settingsDirectory, mddConfig.MddPath));
                Console.WriteLine($"Resolved MDD path: {mddConfig.MddPath}");
            }

            // Resolve project paths in builders
            foreach (var builder in mddConfig.Builders)
            {
                if (builder.Config.TryGetValue("projectPath", out var projectPathElement))
                {
                    string projectPath = projectPathElement.GetString();
                    if (!string.IsNullOrEmpty(projectPath) && !Path.IsPathRooted(projectPath))
                    {
                        string absoluteProjectPath = Path.GetFullPath(Path.Combine(settingsDirectory, projectPath));
                        builder.Config["projectPath"] = JsonDocument.Parse($"\"{absoluteProjectPath.Replace("\\", "\\\\")}\"").RootElement;
                        Console.WriteLine($"Resolved project path: {absoluteProjectPath}");
                    }
                }
            }
        }

        // Resolve log file path if it's relative
        if (!string.IsNullOrEmpty(settings.Logging.LogFilePath) && !Path.IsPathRooted(settings.Logging.LogFilePath))
        {
            settings.Logging.LogFilePath = Path.GetFullPath(Path.Combine(settingsDirectory, settings.Logging.LogFilePath));
            Console.WriteLine($"Resolved log file path: {settings.Logging.LogFilePath}");
        }
    }

    /// <summary>
    /// Apply default values to builder configs where properties are not set
    /// </summary>
    private static void ApplyDefaultsToBuilderConfig(BuilderInfo builderInfo)
    {
        if (builderInfo.Type.Equals("MsSql", StringComparison.OrdinalIgnoreCase))
        {
            // Apply MsSql builder defaults
            var defaults = new Dictionary<string, JsonElement>
            {
                ["tablePath"] = JsonDocument.Parse("\"dbo/Tables_\"").RootElement,
                ["generateIndividualFiles"] = JsonDocument.Parse("true").RootElement,
                ["generateCompleteFile"] = JsonDocument.Parse("true").RootElement,
                ["schemaOnly"] = JsonDocument.Parse("false").RootElement,
                ["useCreateIfNotExists"] = JsonDocument.Parse("true").RootElement,
                ["includeIndexes"] = JsonDocument.Parse("true").RootElement,
                ["clearOutputDirectoryBeforeGeneration"] = JsonDocument.Parse("true").RootElement
            };

            // Add default values for any missing properties
            foreach (var kvp in defaults)
            {
                if (!builderInfo.Config.ContainsKey(kvp.Key))
                {
                    builderInfo.Config[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    /// <summary>
    /// Save settings to a JSON file
    /// </summary>
    public static void SaveSettings(string filePath, Settings settings)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };

        string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Create a default MDD configuration
    /// </summary>
    private static MddConfig CreateDefaultMddConfig()
    {
        return new MddConfig
        {
            MddPath = string.Empty, // Empty string instead of hardcoded path
            Builders = new List<BuilderInfo>
            {
                new BuilderInfo
                {
                    Type = "MsSql",
                    Config = new Dictionary<string, JsonElement>
                    {
                        ["projectPath"] = JsonDocument.Parse("\"\"").RootElement, // Empty string instead of hardcoded path
                        ["tablePath"] = JsonDocument.Parse("\"dbo/Tables_\"").RootElement,
                        ["generateIndividualFiles"] = JsonDocument.Parse("true").RootElement,
                        ["generateCompleteFile"] = JsonDocument.Parse("true").RootElement,
                        ["schemaOnly"] = JsonDocument.Parse("false").RootElement,
                        ["useCreateIfNotExists"] = JsonDocument.Parse("true").RootElement,
                        ["includeIndexes"] = JsonDocument.Parse("true").RootElement,
                        ["clearOutputDirectoryBeforeGeneration"] = JsonDocument.Parse("true").RootElement
                    }
                }
            }
        };
    }

    /// <summary>
    /// Create default settings
    /// </summary>
    private static Settings CreateDefaultSettings()
    {
        return new Settings
        {
            MddConfigs = new List<MddConfig>
            {
                CreateDefaultMddConfig()
            },
            Logging = new LoggingSettings
            {
                Verbose = true,
                LogFilePath = @"mddbooster.log"
            }
        };
    }
}
