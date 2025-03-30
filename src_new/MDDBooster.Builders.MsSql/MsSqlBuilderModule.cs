using MDDBooster.Models;

namespace MDDBooster.Builders.MsSql;

/// <summary>
/// Registration module for MsSql Builder components
/// </summary>
public static class MsSqlBuilderModule
{
    /// <summary>
    /// Registers all MsSql Builder components with the MDDParserOptions
    /// </summary>
    public static MDDParserOptions UseMsSqlBuilder(this MDDParserOptions options)
    {
        // Register framework attribute parsers
        options.FrameworkAttributeParsers.Add(new MsSqlAttributeParser());
        options.FrameworkAttributeParsers.Add(new InsertAttributeParser());
        options.FrameworkAttributeParsers.Add(new UpdateAttributeParser());

        // Register model processors
        options.ModelProcessors.Add(new MsSqlFrameworkAttributeProcessor());

        return options;
    }

    /// <summary>
    /// Creates an MsSql script generator for the specified MDDDocument
    /// </summary>
    public static MsSqlScriptGenerator CreateScriptGenerator(this MDDDocument document, bool useSchemaNamespace = true, string schemaNameOverride = "dbo")
    {
        return new MsSqlScriptGenerator(document, useSchemaNamespace, schemaNameOverride);
    }
}