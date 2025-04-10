using MDDBooster.Models;

namespace MDDBooster.Builders.ModelProject;

/// <summary>
/// Builder for creating ModelGenerator instances with fluent configuration
/// </summary>
public class ModelGeneratorBuilder
{
    private readonly MDDDocument _document;
    private readonly ModelProjectConfig _config = new ModelProjectConfig();

    public ModelGeneratorBuilder(MDDDocument document)
    {
        _document = document;
    }

    /// <summary>
    /// Sets the root namespace for generated code
    /// </summary>
    public ModelGeneratorBuilder WithNamespace(string ns)
    {
        _config.Namespace = ns;
        return this;
    }

    /// <summary>
    /// Sets the path for model classes
    /// </summary>
    public ModelGeneratorBuilder WithModelsPath(string path)
    {
        _config.ModelsPath = path;
        return this;
    }

    /// <summary>
    /// Sets the path for interfaces
    /// </summary>
    public ModelGeneratorBuilder WithInterfacesPath(string path)
    {
        _config.InterfacesPath = path;
        return this;
    }

    /// <summary>
    /// Sets the path for enums
    /// </summary>
    public ModelGeneratorBuilder WithEnumsPath(string path)
    {
        _config.EnumsPath = path;
        return this;
    }

    /// <summary>
    /// Configures whether to generate navigation properties
    /// </summary>
    public ModelGeneratorBuilder WithNavigationProperties(bool generate = true)
    {
        _config.GenerateNavigationProperties = generate;
        return this;
    }

    /// <summary>
    /// Configures whether to generate partial classes
    /// </summary>
    public ModelGeneratorBuilder WithPartialClasses(bool usePartial = true)
    {
        _config.UsePartialClasses = usePartial;
        return this;
    }

    /// <summary>
    /// Configures whether to use nullable reference types
    /// </summary>
    public ModelGeneratorBuilder WithNullableReferenceTypes(bool useNullableRefs = true)
    {
        _config.UseNullableReferenceTypes = useNullableRefs;
        return this;
    }

    /// <summary>
    /// Configures whether to use DateTimeOffset instead of DateTime
    /// </summary>
    public ModelGeneratorBuilder WithDateTimeOffset(bool useDateTimeOffset = true)
    {
        _config.UseDateTimeOffset = useDateTimeOffset;
        return this;
    }

    /// <summary>
    /// Configures whether to implement INotifyPropertyChanged
    /// </summary>
    public ModelGeneratorBuilder WithPropertyChangeNotification(bool implement = true)
    {
        _config.ImplementINotifyPropertyChanged = implement;
        return this;
    }

    /// <summary>
    /// Builds the ModelGenerator with the configured settings
    /// </summary>
    public ModelGenerator Build()
    {
        return new ModelGenerator(_document, _config);
    }
}