using M3LParser.Logging;
using MDDBooster.Models;
using MDDBooster.Utilities;

namespace MDDBooster.Builders.ModelProject;

/// <summary>
/// Builder that generates C# entity model classes
/// </summary>
public class ModelProjectBuilder : IBuilder
{
    public string BuilderType => "ModelProject";

    /// <summary>
    /// Create a default configuration for this builder
    /// </summary>
    public IBuilderConfig CreateDefaultConfig()
    {
        return new ModelProjectConfig
        {
            ProjectPath = string.Empty,
            Namespace = "YourNamespace",
            ModelsPath = "Entity_",       // Changed to use underscore suffix
            InterfacesPath = "Models",
            EnumsPath = "Models",
            GenerateNavigationProperties = true,
            UsePartialClasses = true,
            ImplementINotifyPropertyChanged = false,
            UseDateTimeOffset = false,
            UseNullableReferenceTypes = true,
            DefaultStringLength = 50,
            Cleanup = true
        };
    }

    /// <summary>
    /// Process an MDD document with the provided configuration
    /// </summary>
    public bool Process(MDDDocument document, IBuilderConfig config)
    {
        AppLog.Information("Starting ModelProject builder processing");

        if (!(config is ModelProjectConfig modelConfig))
        {
            AppLog.Error("Invalid configuration type for ModelProjectBuilder");
            return false;
        }

        try
        {
            // Clear model utility cache before processing
            ModelUtilities.ClearFieldsCache();

            // Create generator with the given configuration
            var generator = new ModelGenerator(document, modelConfig);

            // Clean up directories if configured
            if (modelConfig.Cleanup)
            {
                CleanupDirectories(modelConfig);
            }

            // Generate model classes
            GenerateModelClasses(generator, modelConfig);

            // Generate interfaces
            GenerateInterfaces(generator, modelConfig);

            // Generate enums
            GenerateEnums(generator, modelConfig);

            AppLog.Information("ModelProject builder processing completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            AppLog.Error(ex, "Error generating model project");
            return false;
        }
    }

    /// <summary>
    /// Generate C# model classes
    /// </summary>
    private void GenerateModelClasses(ModelGenerator generator, ModelProjectConfig config)
    {
        AppLog.Information("Generating model classes");

        // Get output directory for models
        string outputDir = Path.Combine(config.ProjectPath, config.ModelsPath);
        EnsureDirectoryExists(outputDir);

        // Generate code for each model (excluding abstract models if configured)
        foreach (var model in generator.Document.Models)
        {
            ErrorHandling.ExecuteSafely(() => {
                // Skip abstract models if configured
                if (model.BaseModel.IsAbstract && !config.GenerateAbstractModels)
                {
                    AppLog.Debug("Skipping abstract model: {ModelName}", model.BaseModel.Name);
                    return;
                }

                // Generate model class code
                string modelCode = generator.GenerateModelClass(model);
                if (string.IsNullOrEmpty(modelCode))
                {
                    AppLog.Warning("Empty model code generated for {ModelName}", model.BaseModel.Name);
                    return;
                }

                // Write to file
                string fileName = $"{model.BaseModel.Name}.cs";
                string outputPath = Path.Combine(outputDir, fileName);
                File.WriteAllText(outputPath, modelCode);

                AppLog.Information("Generated model class: {FileName} in directory {Directory}",
                    fileName, outputDir);
            }, "Failed to generate model {ModelName}", model.BaseModel.Name);
        }
    }

    /// <summary>
    /// Generate C# interfaces
    /// </summary>
    private void GenerateInterfaces(ModelGenerator generator, ModelProjectConfig config)
    {
        // Skip generating interfaces if configured not to
        if (!config.GenerateInterface)
        {
            AppLog.Information("Skipping interface generation as per configuration");
            return;
        }

        AppLog.Information("Generating interfaces");

        // Get output directory for interfaces
        string outputDir = Path.Combine(config.ProjectPath, config.InterfacesPath);
        EnsureDirectoryExists(outputDir);

        // Generate code for each interface
        foreach (var iface in generator.Document.Interfaces)
        {
            ErrorHandling.ExecuteSafely(() => {
                // Generate interface code
                string interfaceCode = generator.GenerateInterface(iface);
                if (string.IsNullOrEmpty(interfaceCode))
                {
                    AppLog.Warning("Empty interface code generated for {InterfaceName}", iface.BaseInterface.Name);
                    return;
                }

                // Write to file
                string fileName = $"{iface.BaseInterface.Name}.cs";
                string outputPath = Path.Combine(outputDir, fileName);
                File.WriteAllText(outputPath, interfaceCode);

                AppLog.Information("Generated interface: {FileName}", fileName);
            }, "Failed to generate interface {InterfaceName}", iface.BaseInterface.Name);
        }
    }

    /// <summary>
    /// Generate C# enums
    /// </summary>
    private void GenerateEnums(ModelGenerator generator, ModelProjectConfig config)
    {
        AppLog.Information("Generating enums");

        // Get output directory for enums
        string outputDir = Path.Combine(config.ProjectPath, config.EnumsPath);
        EnsureDirectoryExists(outputDir);

        // Generate code for each enum
        foreach (var enum_ in generator.Document.Enums)
        {
            ErrorHandling.ExecuteSafely(() => {
                // Generate enum code
                string enumCode = generator.GenerateEnum(enum_);
                if (string.IsNullOrEmpty(enumCode))
                {
                    AppLog.Warning("Empty enum code generated for {EnumName}", enum_.BaseEnum.Name);
                    return;
                }

                // Write to file
                string fileName = $"{enum_.BaseEnum.Name}.cs";
                string outputPath = Path.Combine(outputDir, fileName);
                File.WriteAllText(outputPath, enumCode);

                AppLog.Information("Generated enum: {FileName} in directory {Directory}",
                    fileName, outputDir);
            }, "Failed to generate enum {EnumName}", enum_.BaseEnum.Name);
        }
    }

    /// <summary>
    /// Clean up output directories before generation
    /// </summary>
    private void CleanupDirectories(ModelProjectConfig config)
    {
        ErrorHandling.ExecuteSafely(() => {
            // Clean models directory
            string modelsDir = Path.Combine(config.ProjectPath, config.ModelsPath);
            if (Directory.Exists(modelsDir))
            {
                AppLog.Information("Cleaning up models directory: {Directory}", modelsDir);
                ClearDirectory(modelsDir);
            }

            // Clean interfaces directory if generating interfaces
            if (config.GenerateInterface)
            {
                string interfacesDir = Path.Combine(config.ProjectPath, config.InterfacesPath);
                if (Directory.Exists(interfacesDir))
                {
                    AppLog.Information("Cleaning up interfaces directory: {Directory}", interfacesDir);
                    ClearDirectory(interfacesDir);
                }
            }

            // Clean enums directory
            string enumsDir = Path.Combine(config.ProjectPath, config.EnumsPath);
            // Only cleanup if it's a different directory than models
            if (enumsDir != modelsDir && Directory.Exists(enumsDir))
            {
                AppLog.Information("Cleaning up enums directory: {Directory}", enumsDir);
                ClearDirectory(enumsDir);
            }
        }, "Error cleaning up directories");
    }

    /// <summary>
    /// Clears all files in a directory but leaves the directory structure intact
    /// </summary>
    private void ClearDirectory(string directory)
    {
        ErrorHandling.ExecuteSafely(() => {
            // Only delete .cs files and leave any other files/folders intact
            string[] files = Directory.GetFiles(directory, "*.cs");
            int deletedCount = 0;

            foreach (string file in files)
            {
                try
                {
                    File.Delete(file);
                    deletedCount++;
                    AppLog.Debug("Deleted file: {FilePath}", file);
                }
                catch (Exception ex)
                {
                    AppLog.Warning(ex, "Failed to delete file: {FilePath}", file);
                }
            }

            AppLog.Information("Cleaned {Count} files from directory: {Directory}", deletedCount, directory);
        }, "Error clearing directory: {Directory}", directory);
    }

    /// <summary>
    /// Ensure directory exists, create if it doesn't
    /// </summary>
    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            AppLog.Debug("Creating directory: {DirectoryPath}", path);
            Directory.CreateDirectory(path);
        }
    }
}