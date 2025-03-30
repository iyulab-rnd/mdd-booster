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
    /// Load settings from a JSON file
    /// </summary>
    /// <param name="filePath">Path to the settings JSON file</param>
    /// <returns>Loaded settings</returns>
    public static Settings Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            var defaultSettings = CreateDefaultSettings();
            SaveSettings(filePath, defaultSettings);
            return defaultSettings;
        }

        string json = File.ReadAllText(filePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        try
        {
            var settings = JsonSerializer.Deserialize<Settings>(json, options);

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
                }

                return settings;
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing settings file: {ex.Message}");
            Console.WriteLine("Using default settings instead.");
        }

        return CreateDefaultSettings();
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

/// <summary>
/// Configuration for an MDD file to process
/// </summary>
public class MddConfig
{
    /// <summary>
    /// Path to the MDD file
    /// </summary>
    public string MddPath { get; set; } = string.Empty;

    /// <summary>
    /// List of builders to apply to this MDD file
    /// </summary>
    public List<BuilderInfo> Builders { get; set; } = new List<BuilderInfo>();
}

/// <summary>
/// Information about a builder to apply
/// </summary>
public class BuilderInfo
{
    /// <summary>
    /// Type of builder
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Builder configuration (dynamic)
    /// </summary>
    public Dictionary<string, JsonElement> Config { get; set; } = new Dictionary<string, JsonElement>();
}

/// <summary>
/// Logging settings
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Enable verbose logging
    /// </summary>
    public bool Verbose { get; set; } = false;

    /// <summary>
    /// Path to the log file (if empty, logs to console only)
    /// </summary>
    public string LogFilePath { get; set; } = string.Empty;
}