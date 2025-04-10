﻿namespace M3LParser.Parsers;

/// <summary>
/// Parser for field definitions
/// </summary>
public class FieldParser : BaseParser
{
    public FieldParser(ParserContext context) : base(context)
    {
    }

    /// <summary>
    /// Parse a field definition
    /// </summary>
    public M3LField Parse()
    {
        var currentLine = Context.CurrentLineTrimmed;

        // If not a field definition, return null
        if (!currentLine.StartsWith("-"))
        {
            return null;
        }

        var field = new M3LField();
        AppLog.Debug("Parsing field at line {LineNumber}", Context.CurrentLineIndex + 1);

        // Remove the leading dash
        var fieldContent = currentLine.Substring(1).Trim();

        // Check if it's a simple field definition
        if (fieldContent.Contains(':'))
        {
            ParseSimpleField(field, fieldContent);
        }
        else
        {
            // It's an extended field definition
            ParseExtendedField(field, fieldContent);
        }

        return field;
    }

    /// <summary>
    /// Parse a simple single-line field definition
    /// </summary>
    private void ParseSimpleField(M3LField field, string fieldContent)
    {
        var parts = fieldContent.Split(':', 2);
        field.Name = parts[0].Trim();

        var typePart = parts[1].Trim();
        AppLog.Debug("Parsing simple field: {FieldName}", field.Name);

        // First extract framework attributes (in square brackets) before processing default values
        // This ensures framework attributes aren't mistakenly treated as default values
        if (typePart.Contains('[') && typePart.Contains(']'))
        {
            var matches = Regex.Matches(typePart, @"\[([^\]]+)\]");
            foreach (Match match in matches)
            {
                field.FrameworkAttributes.Add(match.Groups[1].Value);
                AppLog.Debug("Field {FieldName} has framework attribute: {Attribute}", field.Name, match.Groups[1].Value);

                // Remove the framework attribute from the type part to avoid confusion with default values
                typePart = typePart.Replace(match.Value, "").Trim();
            }
        }

        // Parse default value AFTER framework attributes are removed
        if (typePart.Contains('='))
        {
            var defaultParts = typePart.Split('=', 2);
            typePart = defaultParts[0].Trim();
            field.DefaultValue = defaultParts[1].Trim();
            if (field.DefaultValue.StartsWith('\"') && field.DefaultValue.EndsWith('\"'))
            {
                field.DefaultValue = field.DefaultValue[1..^1];
            }
            AppLog.Debug("Field {FieldName} has default value: {DefaultValue}", field.Name, field.DefaultValue);
        }

        // Parse attributes
        if (typePart.Contains('@'))
        {
            var attributeParts = typePart.Split('@', StringSplitOptions.RemoveEmptyEntries);
            typePart = attributeParts[0].Trim();

            for (int i = 1; i < attributeParts.Length; i++)
            {
                var attr = "@" + attributeParts[i].Trim();
                field.Attributes.Add(attr);
                AppLog.Debug("Field {FieldName} has attribute: {Attribute}", field.Name, attr);
            }
        }

        // Parse type and nullable (after all other components are extracted)
        if (typePart.EndsWith("?"))
        {
            field.IsNullable = true;
            typePart = typePart.Substring(0, typePart.Length - 1);
            AppLog.Debug("Field {FieldName} is nullable", field.Name);
        }

        // Parse type and length
        if (typePart.Contains('(') && typePart.Contains(')'))
        {
            var match = Regex.Match(typePart, @"(.+?)\((.+?)\)");
            if (match.Success)
            {
                field.Type = match.Groups[1].Value.Trim();
                field.Length = match.Groups[2].Value.Trim();
                AppLog.Debug("Field {FieldName} has type {FieldType} with length {Length}",
                    field.Name, field.Type, field.Length);
            }
        }
        else
        {
            field.Type = typePart;
            AppLog.Debug("Field {FieldName} has type {FieldType}", field.Name, field.Type);
        }

        // Check next line for description
        if (Context.NextLine())
        {
            var nextLine = Context.CurrentLineTrimmed;
            if (nextLine.StartsWith(">"))
            {
                field.Description = nextLine.Substring(1).Trim();
                AppLog.Debug("Field {FieldName} has description: {Description}", field.Name, field.Description);
                Context.NextLine();
            }
        }
    }

    /// <summary>
    /// Parse an extended multi-line field definition
    /// </summary>
    private void ParseExtendedField(M3LField field, string fieldContent)
    {
        field.Name = fieldContent;
        AppLog.Debug("Parsing extended field definition: {FieldName}", field.Name);

        // Move to the next line for extended properties
        if (!Context.NextLine())
            return;

        // Parse the extended field properties
        while (Context.HasMoreLines)
        {
            var subLine = Context.CurrentLineTrimmed;

            // End of extended field definition - must be a line that doesn't start with a dash 
            // or doesn't have a nested dash after the initial dash
            if (!subLine.StartsWith("-") || !subLine.Substring(1).TrimStart().StartsWith("-"))
            {
                break;
            }

            // Parse subproperty - remove the leading dashes and trim
            var subProperty = subLine.Substring(1).Trim().Substring(1).Trim();

            // Check for framework attributes first (they should be prioritized over other properties)
            if (subProperty.StartsWith("[") && subProperty.Contains("]"))
            {
                var matches = Regex.Matches(subProperty, @"\[([^\]]+)\]");
                foreach (Match match in matches)
                {
                    field.FrameworkAttributes.Add(match.Groups[1].Value);
                    AppLog.Debug("Field {FieldName} has framework attribute: {Attribute}", field.Name, match.Groups[1].Value);
                }
            }
            else if (subProperty.StartsWith("type:"))
            {
                ParseTypeProperty(field, subProperty);
            }
            else if (subProperty.StartsWith("description:"))
            {
                field.Description = ExtractStringValue(subProperty, "description:");
                AppLog.Debug("Field {FieldName} has description: {Description}", field.Name, field.Description);
            }
            else if (subProperty.StartsWith("default:"))
            {
                field.DefaultValue = subProperty.Substring("default:".Length).Trim();
                AppLog.Debug("Field {FieldName} has default value: {DefaultValue}", field.Name, field.DefaultValue);
            }
            else if (subProperty.Contains('=') && !subProperty.StartsWith("["))
            {
                // Handle key=value syntax for properties other than framework attributes
                var parts = subProperty.Split('=', 2);
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (key.Equals("default", StringComparison.OrdinalIgnoreCase))
                {
                    field.DefaultValue = value;
                    AppLog.Debug("Field {FieldName} has default value: {DefaultValue}", field.Name, field.DefaultValue);
                }
            }
            else
            {
                // Assume it's an attribute
                ParseAttributeProperty(field, subProperty);
            }

            Context.NextLine();
        }
    }

    /// <summary>
    /// Parse the type property of an extended field
    /// </summary>
    private void ParseTypeProperty(M3LField field, string typeProperty)
    {
        var typePart = typeProperty.Substring("type:".Length).Trim();

        // Parse type and nullable
        if (typePart.EndsWith("?"))
        {
            field.IsNullable = true;
            typePart = typePart.Substring(0, typePart.Length - 1);
            AppLog.Debug("Field {FieldName} is nullable", field.Name);
        }

        // Parse type and length
        if (typePart.Contains('(') && typePart.Contains(')'))
        {
            var match = Regex.Match(typePart, @"(.+?)\((.+?)\)");
            if (match.Success)
            {
                field.Type = match.Groups[1].Value.Trim();
                field.Length = match.Groups[2].Value.Trim();
                AppLog.Debug("Field {FieldName} has type {FieldType} with length {Length}",
                    field.Name, field.Type, field.Length);
            }
        }
        else
        {
            field.Type = typePart;
            AppLog.Debug("Field {FieldName} has type {FieldType}", field.Name, field.Type);
        }
    }

    /// <summary>
    /// Parse an attribute property
    /// </summary>
    private void ParseAttributeProperty(M3LField field, string attributeProperty)
    {
        var attributeParts = attributeProperty.Split(':', 2);
        var attributeName = attributeParts[0].Trim();

        if (attributeParts.Length > 1)
        {
            var attr = $"@{attributeName}({attributeParts[1].Trim()})";
            field.Attributes.Add(attr);
            AppLog.Debug("Field {FieldName} has attribute: {Attribute}", field.Name, attr);
        }
        else
        {
            var attr = $"@{attributeName}";
            field.Attributes.Add(attr);
            AppLog.Debug("Field {FieldName} has attribute: {Attribute}", field.Name, attr);
        }
    }

    /// <summary>
    /// Extract a string value from a property, removing quotes if present
    /// </summary>
    private string ExtractStringValue(string property, string prefix)
    {
        var value = property.Substring(prefix.Length).Trim();

        // Remove quotes if present
        if (value.StartsWith("\"") && value.EndsWith("\""))
        {
            value = value.Substring(1, value.Length - 2);
        }

        return value;
    }
}