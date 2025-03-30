namespace M3LParser.Helpers;

/// <summary>
/// Helper methods for string manipulation
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Extract the name part from a definition line
    /// </summary>
    /// <param name="line">The definition line</param>
    /// <returns>The extracted name</returns>
    public static string ExtractName(string line)
    {
        if (string.IsNullOrEmpty(line))
            return string.Empty;

        // Remove leading ## if present
        if (line.StartsWith("##"))
            line = line.Substring(2).Trim();

        // Extract name before any special characters
        int endOfName = line.IndexOfAny(new[] { '(', ':', '#', '@', ' ' });
        if (endOfName == -1)
            return line.Trim();

        return line.Substring(0, endOfName).Trim();
    }

    /// <summary>
    /// Extract the label part from a definition line (text in parentheses)
    /// </summary>
    /// <param name="line">The definition line</param>
    /// <returns>The extracted label or null if not found</returns>
    public static string ExtractLabel(string line)
    {
        if (string.IsNullOrEmpty(line))
            return null;

        var match = Regex.Match(line, @"\((.+?)\)");
        if (match.Success)
            return match.Groups[1].Value.Trim();

        return null;
    }

    /// <summary>
    /// Extract the inheritance part from a definition line (text after colon)
    /// </summary>
    /// <param name="line">The definition line</param>
    /// <returns>List of inherited types</returns>
    public static List<string> ExtractInheritance(string line)
    {
        if (string.IsNullOrEmpty(line) || !line.Contains(':'))
            return new List<string>();

        // Find the colon
        int colonIndex = line.IndexOf(':');

        // Check for special keywords after colon (::interface, ::enum)
        if (colonIndex + 1 < line.Length && line[colonIndex + 1] == ':')
            return new List<string>();

        // Extract the inheritance part
        var inheritancePart = line.Substring(colonIndex + 1);

        // Remove any trailing parts (after #, @, etc.)
        int endOfInheritance = inheritancePart.IndexOfAny(new[] { '#', '@' });
        if (endOfInheritance != -1)
            inheritancePart = inheritancePart.Substring(0, endOfInheritance);

        // Split by comma and trim
        return inheritancePart.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList();
    }

    /// <summary>
    /// Extract the description part from a definition line (text after # symbol)
    /// </summary>
    /// <param name="line">The definition line</param>
    /// <returns>The extracted description or null if not found</returns>
    public static string ExtractDescription(string line)
    {
        if (string.IsNullOrEmpty(line) || !line.Contains('#'))
            return null;

        // This handles # as a comment marker, not ## as a heading marker
        int hashIndex = line.IndexOf('#');
        if (hashIndex == 0 || (hashIndex > 0 && line[hashIndex - 1] == '#'))
            return null;

        return line.Substring(hashIndex + 1).Trim();
    }

    /// <summary>
    /// Extract attributes from a definition line (text after @ symbol)
    /// </summary>
    /// <param name="line">The definition line</param>
    /// <returns>List of extracted attributes</returns>
    public static List<string> ExtractAttributes(string line)
    {
        var attributes = new List<string>();
        if (string.IsNullOrEmpty(line) || !line.Contains('@'))
            return attributes;

        var parts = line.Split('@', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < parts.Length; i++)
        {
            var attrText = parts[i].Trim();
            var endOfAttr = attrText.IndexOfAny(new[] { ' ', '\t', '(', ')' });

            if (endOfAttr > 0)
            {
                if (attrText[endOfAttr] == '(')
                {
                    // Find the closing parenthesis
                    int closeParenIndex = attrText.IndexOf(')', endOfAttr);
                    if (closeParenIndex != -1)
                        attrText = attrText.Substring(0, closeParenIndex + 1);
                    else
                        attrText = attrText.Substring(0, endOfAttr);
                }
                else
                {
                    attrText = attrText.Substring(0, endOfAttr);
                }
            }

            attributes.Add('@' + attrText);
        }

        return attributes;
    }

    /// <summary>
    /// Extract framework attributes from a definition line (text in square brackets)
    /// </summary>
    /// <param name="line">The definition line</param>
    /// <returns>List of extracted framework attributes</returns>
    public static List<string> ExtractFrameworkAttributes(string line)
    {
        var attributes = new List<string>();
        if (string.IsNullOrEmpty(line) || !line.Contains('['))
            return attributes;

        var matches = Regex.Matches(line, @"\[([^\]]+)\]");
        foreach (Match match in matches)
        {
            attributes.Add(match.Groups[1].Value);
        }

        return attributes;
    }

    /// <summary>
    /// Normalize a name according to common naming conventions
    /// </summary>
    /// <param name="name">The name to normalize</param>
    /// <param name="pascalCase">Whether to convert to PascalCase</param>
    /// <returns>The normalized name</returns>
    public static string NormalizeName(string name, bool pascalCase = true)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Just log the name before normalization for debugging
        AppLog.Debug("Normalizing name: '{Name}'", name);

        // Remove invalid characters
        var validChars = new List<char>();
        foreach (char c in name)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
                validChars.Add(c);
            else if (char.IsWhiteSpace(c))
                validChars.Add('_');
        }

        name = new string(validChars.ToArray());

        // Convert to PascalCase if requested
        if (pascalCase && name.Length > 0)
        {
            // Split by underscores
            var parts = name.Split('_', StringSplitOptions.RemoveEmptyEntries);

            // Capitalize first letter of each part
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
                }
            }

            name = string.Join("", parts);
        }

        AppLog.Debug("Normalized name: '{NormalizedName}'", name);
        return name;
    }
}