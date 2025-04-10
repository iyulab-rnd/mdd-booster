namespace MDDBooster.Builders.ModelProject;

/// <summary>
/// Configuration for the ModelProject builder
/// </summary>
public class ModelProjectConfig : IBuilderConfig
{
    /// <summary>
    /// Path to the project directory
    /// </summary>
    public string ProjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Root namespace for the generated code
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Relative path for model classes (from project path)
    /// </summary>
    public string ModelsPath { get; set; } = "Entity";

    /// <summary>
    /// Relative path for interfaces (from project path)
    /// </summary>
    public string InterfacesPath { get; set; } = "Models";

    /// <summary>
    /// Relative path for enums (from project path)
    /// </summary>
    public string EnumsPath { get; set; } = "Models";

    /// <summary>
    /// Generate navigation properties for relationships
    /// </summary>
    public bool GenerateNavigationProperties { get; set; } = true;

    /// <summary>
    /// Generate interface files
    /// </summary>
    public bool GenerateInterface { get; set; } = true;

    /// <summary>
    /// Generate code for abstract models
    /// </summary>
    public bool GenerateAbstractModels { get; set; } = true;

    /// <summary>
    /// Generate models as partial classes
    /// </summary>
    public bool UsePartialClasses { get; set; } = true;

    /// <summary>
    /// Implement INotifyPropertyChanged for all models
    /// </summary>
    public bool ImplementINotifyPropertyChanged { get; set; } = false;

    /// <summary>
    /// Use DateTimeOffset instead of DateTime
    /// </summary>
    public bool UseDateTimeOffset { get; set; } = false;

    /// <summary>
    /// Use C# nullable reference types features
    /// </summary>
    public bool UseNullableReferenceTypes { get; set; } = true;

    /// <summary>
    /// Default string length when not specified
    /// </summary>
    public int DefaultStringLength { get; set; } = 50;

    /// <summary>
    /// Clean up output directories before generating files
    /// </summary>
    public bool Cleanup { get; set; } = true;

    /// <summary>
    /// Get the full output path for this builder
    /// </summary>
    public string GetFullOutputPath()
    {
        return ProjectPath;
    }
}