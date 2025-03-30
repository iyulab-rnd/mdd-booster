namespace M3LParser.Helpers;

/// <summary>
/// Helper methods and constants for regular expressions
/// </summary>
public static class RegexHelper
{
    /// <summary>
    /// Pattern to match a model definition line
    /// </summary>
    public static readonly Regex ModelDefinitionPattern = new Regex(
        @"^##\s+([^(:@#]+)(?:\s*\(([^)]+)\))?(?:\s*:\s*([^@#]+))?(?:\s*@(.+))?(?:\s*#(.+))?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Pattern to match an interface or enum definition line
    /// </summary>
    public static readonly Regex SpecialDefinitionPattern = new Regex(
        @"^##\s+([^:]+)(?:\s*:\s*([^:]+))?(?:\s*::(\w+))(?:\s*#(.+))?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Pattern to match a field definition
    /// </summary>
    public static readonly Regex FieldDefinitionPattern = new Regex(
        @"^-\s+([^:]+):\s*([^\s=@\[]+)(?:\(([^)]+)\))?(\?)?\s*(?:=\s*([^@\[]+))?(?:\s*@(.+))?(?:\s*\[(.+)\])?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Pattern to match an enum value
    /// </summary>
    public static readonly Regex EnumValuePattern = new Regex(
        @"^-\s+([^:]+)(?::\s*(?:(\w+)\s*=\s*([^""]+))?(?:\s*""([^""]+)"")?)?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Pattern to match a relation definition
    /// </summary>
    public static readonly Regex RelationPattern = new Regex(
        @"^-\s+([<>])([^""]+)(?:\s+""([^""]+)"")?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Pattern to match a model-level relation
    /// </summary>
    public static readonly Regex ModelLevelRelationPattern = new Regex(
        @"^-\s+@relation\(([^,]+),\s*([<>]-)\s*([^,)]+)(?:,\s*from:\s*([^)]+))?(?:,\s*(.+))?\)(?:\s+""([^""]+)"")?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Pattern to match a model-level index
    /// </summary>
    public static readonly Regex ModelLevelIndexPattern = new Regex(
        @"^-\s+@(index|unique)\(([^)]+)\)(?:\s+""([^""]+)"")?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Pattern to match a metadata definition
    /// </summary>
    public static readonly Regex MetadataPattern = new Regex(
        @"^-\s+([^:]+)(?::\s*(.+))?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Extract a match group value safely
    /// </summary>
    /// <param name="match">The regex match</param>
    /// <param name="groupIndex">The group index</param>
    /// <returns>The group value or empty string if not matched</returns>
    public static string GetGroupValue(Match match, int groupIndex)
    {
        if (!match.Success || groupIndex >= match.Groups.Count)
            return string.Empty;

        return match.Groups[groupIndex].Value;
    }

    /// <summary>
    /// Extract a parameter value from an attribute
    /// </summary>
    /// <param name="attribute">The attribute string</param>
    /// <param name="attributeName">The name of the attribute</param>
    /// <returns>The parameter value or null if not found</returns>
    public static string ExtractAttributeParameter(string attribute, string attributeName)
    {
        if (string.IsNullOrEmpty(attribute) || !attribute.StartsWith('@' + attributeName))
            return null;

        var match = Regex.Match(attribute, $@"@{attributeName}\(([^)]+)\)");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Check if an attribute is present (without parameters)
    /// </summary>
    /// <param name="attribute">The attribute string</param>
    /// <param name="attributeName">The name of the attribute</param>
    /// <returns>True if the attribute is present</returns>
    public static bool HasAttribute(string attribute, string attributeName)
    {
        if (string.IsNullOrEmpty(attribute))
            return false;

        return attribute == '@' + attributeName || attribute.StartsWith('@' + attributeName + '(');
    }
}