namespace M3LParser.Parsers;

/// <summary>
/// Parser for model definitions
/// </summary>
public class ModelParser : BaseParser
{
    private readonly FieldParser _fieldParser;
    private readonly RelationParser _relationParser;
    private readonly IndexParser _indexParser;
    private readonly MetadataParser _metadataParser;

    /// <summary>
    /// Initialize a new model parser
    /// </summary>
    /// <param name="context">Parsing context</param>
    public ModelParser(ParserContext context) : base(context)
    {
        _fieldParser = new FieldParser(context);
        _relationParser = new RelationParser(context);
        _indexParser = new IndexParser(context);
        _metadataParser = new MetadataParser(context);
    }

    /// <summary>
    /// Parse a model definition
    /// </summary>
    /// <param name="definitionLine">Line containing the model definition</param>
    /// <returns>The parsed model</returns>
    public M3LModel Parse(string definitionLine)
    {
        var model = new M3LModel();

        AppLog.Debug("Parsing model at line {LineNumber}: {Line}", Context.CurrentLineIndex + 1, definitionLine);

        // Parse main model information (name, label, inheritance, attributes)
        ParseModelDefinition(model, definitionLine);

        // Process model body
        if (!Context.NextLine())
            return model;

        string currentSection = null;

        while (Context.HasMoreLines && !IsEndOfDefinition(Context.CurrentLineTrimmed))
        {
            var currentLine = Context.CurrentLineTrimmed;

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(currentLine) ||
                currentLine.StartsWith("<!--") ||
                currentLine.StartsWith("-->"))
            {
                Context.NextLine();
                continue;
            }

            // Check for section headers
            if (IsStartOfSection(currentLine))
            {
                currentSection = GetSectionName(currentLine);
                AppLog.Debug("Found section: {SectionName}", currentSection);
                Context.NextLine();
                continue;
            }

            // If next line is just text and we don't have a description yet, assume it's the description
            if (!currentLine.StartsWith("-") && !currentLine.StartsWith("#") &&
                string.IsNullOrEmpty(model.Description))
            {
                model.Description = currentLine;
                AppLog.Debug("Model description: {Description}", model.Description);
                Context.NextLine();
                continue;
            }

            // Parse field, relation, index, etc. based on current section
            if (currentLine.StartsWith("-"))
            {
                ParseModelElement(model, currentSection);
                continue;
            }

            // Move to next line if we couldn't parse anything
            Context.NextLine();
        }

        AppLog.Debug("Completed parsing model {ModelName} with {FieldCount} fields, {RelationCount} relations, {IndexCount} indexes",
            model.Name, model.Fields.Count, model.Relations.Count, model.Indexes.Count);

        return model;
    }

    /// <summary>
    /// Parse a model element based on the current section
    /// </summary>
    private void ParseModelElement(M3LModel model, string currentSection)
    {
        switch (currentSection?.ToLowerInvariant())
        {
            case "relations":
                var relation = _relationParser.Parse();
                if (relation != null)
                {
                    AppLog.Debug("Added relation to model {ModelName}: {RelationName} -> {Target}",
                        model.Name, relation.Name, relation.Target);
                    model.Relations.Add(relation);
                }
                break;

            case "indexes":
                var index = _indexParser.Parse();
                if (index != null)
                {
                    AppLog.Debug("Added index to model {ModelName}: {IndexName} (fields: {Fields})",
                        model.Name, index.Name, string.Join(", ", index.Fields));
                    model.Indexes.Add(index);
                }
                break;

            case "metadata":
                var (key, value) = _metadataParser.Parse();
                model.Metadata[key] = value;
                AppLog.Debug("Added metadata to model {ModelName}: {Key} = {Value}",
                    model.Name, key, value);
                break;

            default:
                // If no section is specified, assume it's a field
                var field = _fieldParser.Parse();
                if (field != null)
                {
                    AppLog.Debug("Added field to model {ModelName}: {FieldName} ({FieldType})",
                        model.Name, field.Name, field.Type);
                    model.Fields.Add(field);
                    break;
                }

                // Check if it's an index or relation at model level
                var currentLine = Context.CurrentLineTrimmed;
                if (currentLine.Contains("@index") || currentLine.Contains("@unique"))
                {
                    var modelIndex = ParseModelLevelIndex(currentLine);
                    if (modelIndex != null)
                    {
                        AppLog.Debug("Added model-level index to model {ModelName}: {IndexName} (fields: {Fields})",
                            model.Name, modelIndex.Name, string.Join(", ", modelIndex.Fields));
                        model.Indexes.Add(modelIndex);
                    }
                }
                else if (currentLine.Contains("@relation"))
                {
                    var modelRelation = ParseModelLevelRelation(currentLine);
                    if (modelRelation != null)
                    {
                        AppLog.Debug("Added model-level relation to model {ModelName}: {RelationName} -> {Target}",
                            model.Name, modelRelation.Name, modelRelation.Target);
                        model.Relations.Add(modelRelation);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Parse the model definition line to extract name, label, inheritance, and attributes
    /// </summary>
    private void ParseModelDefinition(M3LModel model, string definitionLine)
    {
        var mainPart = definitionLine;

        AppLog.Debug("Parsing model definition: {Line}", mainPart);

        // Extract attributes
        if (mainPart.Contains('@'))
        {
            var attributeParts = mainPart.Split('@', StringSplitOptions.RemoveEmptyEntries);
            mainPart = attributeParts[0].Trim();

            for (int i = 1; i < attributeParts.Length; i++)
            {
                var attr = "@" + attributeParts[i].Trim();
                model.Attributes.Add(attr);
                AppLog.Debug("Added model attribute: {Attribute}", attr);
            }
        }

        // Extract model name and label
        if (mainPart.Contains('(') && mainPart.Contains(')'))
        {
            var match = Regex.Match(mainPart, @"(.+?)\((.+?)\)(.*)");
            if (match.Success)
            {
                var namePart = match.Groups[1].Value.Trim();
                model.Label = match.Groups[2].Value.Trim();
                var rest = match.Groups[3].Value.Trim();

                ParseModelNameWithInheritance(model, namePart);

                AppLog.Debug("Model name: {ModelName}, label: {Label}", model.Name, model.Label);

                // Check if there's a description after the label
                if (!string.IsNullOrEmpty(rest) && rest.StartsWith("#"))
                {
                    model.Description = rest.Substring(1).Trim();
                    AppLog.Debug("Model description: {Description}", model.Description);
                }
            }
        }
        else if (mainPart.Contains(':'))
        {
            ParseModelNameWithInheritance(model, mainPart);
        }
        else if (mainPart.Contains('#'))
        {
            var descParts = mainPart.Split('#', 2);
            model.Name = descParts[0].Trim();
            model.Description = descParts[1].Trim();
            AppLog.Debug("Model name: {ModelName}, description: {Description}", model.Name, model.Description);
        }
        else
        {
            model.Name = mainPart.Trim();
            AppLog.Debug("Model name: {ModelName}", model.Name);
        }
    }

    /// <summary>
    /// Parse model name with inheritance information
    /// </summary>
    private void ParseModelNameWithInheritance(M3LModel model, string namePart)
    {
        if (namePart.Contains(':'))
        {
            var inheritanceParts = namePart.Split(':', StringSplitOptions.RemoveEmptyEntries);
            model.Name = inheritanceParts[0].Trim();

            if (inheritanceParts.Length > 1)
            {
                var inheritsList = inheritanceParts[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .ToList();

                model.Inherits.AddRange(inheritsList);
                AppLog.Debug("Model {ModelName} inherits from: {InheritanceList}",
                    model.Name, string.Join(", ", model.Inherits));
            }
        }
        else
        {
            model.Name = namePart;
        }
    }

    /// <summary>
    /// Parse an index definition at the model level (using @index or @unique)
    /// </summary>
    private M3LIndex ParseModelLevelIndex(string line)
    {
        var index = new M3LIndex();

        // Check if it's an index
        if (line.Contains("@index("))
        {
            var match = Regex.Match(line, @"@index\(([^)]+)\)");
            if (match.Success)
            {
                AppLog.Debug("Parsing model-level index: {Line}", line);
                var indexParams = match.Groups[1].Value;
                var parameters = ParseParameters(indexParams);

                index.Fields = parameters.Where(p => !p.Contains(":")).ToList();
                index.IsUnique = false;

                // Check for name parameter
                var nameParam = parameters.FirstOrDefault(p => p.StartsWith("name:"));
                if (nameParam != null)
                {
                    index.Name = nameParam.Substring("name:".Length).Trim().Trim('"');
                    AppLog.Debug("Index name from parameter: {Name}", index.Name);
                }
                else if (index.Fields.Count > 0)
                {
                    index.Name = string.Join("_", index.Fields);
                    AppLog.Debug("Generated index name: {Name}", index.Name);
                }

                // Check for description (in quotes after the parentheses)
                var descMatch = Regex.Match(line, @"\)\s+""([^""]+)""");
                if (descMatch.Success)
                {
                    index.Description = descMatch.Groups[1].Value;
                    AppLog.Debug("Index description: {Description}", index.Description);
                }

                return index;
            }
        }
        // Check if it's a unique constraint
        else if (line.Contains("@unique("))
        {
            var match = Regex.Match(line, @"@unique\(([^)]+)\)");
            if (match.Success)
            {
                AppLog.Debug("Parsing model-level unique constraint: {Line}", line);
                var uniqueParams = match.Groups[1].Value;
                var parameters = ParseParameters(uniqueParams);

                index.Fields = parameters.Where(p => !p.Contains(":")).ToList();
                index.IsUnique = true;

                // Check for name parameter
                var nameParam = parameters.FirstOrDefault(p => p.StartsWith("name:"));
                if (nameParam != null)
                {
                    index.Name = nameParam.Substring("name:".Length).Trim().Trim('"');
                    AppLog.Debug("Unique constraint name from parameter: {Name}", index.Name);
                }
                else if (index.Fields.Count > 0)
                {
                    index.Name = "UX_" + string.Join("_", index.Fields);
                    AppLog.Debug("Generated unique constraint name: {Name}", index.Name);
                }

                // Check for description (in quotes after the parentheses)
                var descMatch = Regex.Match(line, @"\)\s+""([^""]+)""");
                if (descMatch.Success)
                {
                    index.Description = descMatch.Groups[1].Value;
                    AppLog.Debug("Unique constraint description: {Description}", index.Description);
                }

                return index;
            }
        }

        return null;
    }

    /// <summary>
    /// Parse a relation definition at the model level (using @relation)
    /// </summary>
    private M3LRelation ParseModelLevelRelation(string line)
    {
        var relation = new M3LRelation();

        // Parse relation definition
        if (line.Contains("@relation("))
        {
            var match = Regex.Match(line, @"@relation\(([^)]+)\)");
            if (match.Success)
            {
                AppLog.Debug("Parsing model-level relation: {Line}", line);
                var relationParams = match.Groups[1].Value;
                var parameters = ParseParameters(relationParams);

                if (parameters.Count >= 2)
                {
                    relation.Name = parameters[0];
                    AppLog.Debug("Relation name: {Name}", relation.Name);

                    // Parse target and direction
                    var targetParam = parameters[1];
                    if (targetParam.StartsWith("->"))
                    {
                        relation.IsToOne = true;
                        relation.Target = targetParam.Substring(2).Trim();
                        AppLog.Debug("Relation is to-one to target: {Target}", relation.Target);
                    }
                    else if (targetParam.StartsWith("<-"))
                    {
                        relation.IsToOne = false;
                        relation.Target = targetParam.Substring(2).Trim();
                        AppLog.Debug("Relation is to-many to target: {Target}", relation.Target);
                    }

                    // Check for from parameter
                    var fromParam = parameters.FirstOrDefault(p => p.StartsWith("from:"));
                    if (fromParam != null)
                    {
                        relation.From = fromParam.Substring("from:".Length).Trim();
                        AppLog.Debug("Relation from field: {FromField}", relation.From);
                    }
                }

                // Check for description (in quotes after the parentheses)
                var descMatch = Regex.Match(line, @"\)\s+""([^""]+)""");
                if (descMatch.Success)
                {
                    relation.Description = descMatch.Groups[1].Value;
                    AppLog.Debug("Relation description: {Description}", relation.Description);
                }

                return relation;
            }
        }

        return null;
    }
}