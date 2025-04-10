using MDDBooster.Builders.ModelProject.Parsers;
using MDDBooster.Builders.ModelProject.Processors;
using MDDBooster.Models;

namespace MDDBooster.Builders.ModelProject;

/// <summary>
/// Registration module for ModelProject Builder components
/// </summary>
public static class ModelProjectModule
{
    /// <summary>
    /// Registers all ModelProject Builder components with the MDDParserOptions
    /// </summary>
    public static MDDParserOptions UseModelProjectBuilder(this MDDParserOptions options)
    {
        // Register framework attribute parsers
        options.FrameworkAttributeParsers.Add(new ModelAttributeParser());

        // Add model processors
        options.ModelProcessors.Add(new ModelAttributeProcessor());

        return options;
    }

    /// <summary>
    /// Creates a model generator for the specified MDDDocument
    /// </summary>
    public static ModelGenerator CreateModelGenerator(
        this MDDDocument document,
        string projectNamespace,
        bool generateNavigationProperties = true,
        bool usePartialClasses = true,
        bool useNullableReferenceTypes = true)
    {
        var config = new ModelProjectConfig
        {
            Namespace = projectNamespace,
            GenerateNavigationProperties = generateNavigationProperties,
            UsePartialClasses = usePartialClasses,
            UseNullableReferenceTypes = useNullableReferenceTypes
        };

        return new ModelGenerator(document, config);
    }

    /// <summary>
    /// Creates a model generator builder for the specified MDDDocument
    /// </summary>
    public static ModelGeneratorBuilder CreateModelGeneratorBuilder(this MDDDocument document)
    {
        return new ModelGeneratorBuilder(document);
    }
}