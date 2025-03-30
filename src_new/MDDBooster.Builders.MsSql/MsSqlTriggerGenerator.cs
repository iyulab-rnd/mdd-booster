using MDDBooster.Models;

namespace MDDBooster.Builders.MsSql;

public class MsSqlTriggerGenerator
{
    private readonly MDDDocument _document;

    public MsSqlTriggerGenerator(MDDDocument document)
    {
        _document = document;
    }

    public string GenerateTriggers()
    {
        var sb = new StringBuilder();

        foreach (var model in _document.Models)
        {
            // Skip abstract models
            if (model.BaseModel.IsAbstract)
            {
                continue;
            }

            GenerateInsertTriggers(sb, model);
            GenerateUpdateTriggers(sb, model);
        }

        return sb.ToString();
    }

    private void GenerateInsertTriggers(StringBuilder sb, MDDModel model)
    {
        // Check if any field has insert values
        var fieldsWithInsertValues = model.Fields
            .Where(f => f.ExtendedMetadata.ContainsKey("InsertValue"))
            .ToList();

        if (fieldsWithInsertValues.Any())
        {
            sb.AppendLine($"CREATE TRIGGER [TR_{model.BaseModel.Name}_Insert]");
            sb.AppendLine($"ON [{_document.BaseDocument.Namespace}].[{model.BaseModel.Name}]");
            sb.AppendLine("INSTEAD OF INSERT");
            sb.AppendLine("AS");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    SET NOCOUNT ON;");
            sb.AppendLine();

            // Build insert statement
            sb.Append("    INSERT INTO [" + _document.BaseDocument.Namespace + "].[" + model.BaseModel.Name + "] (");

            // Get all field names
            var allFields = GetAllFields(model);
            var fieldNames = allFields.Select(f => f.BaseField.Name).ToList();
            sb.AppendLine(string.Join(", ", fieldNames.Select(f => $"[{f}]")) + ")");

            // Build select statement
            sb.Append("    SELECT ");

            // Map fields with their values
            var fieldValues = new List<string>();
            foreach (var field in allFields)
            {
                if (field.ExtendedMetadata.ContainsKey("InsertValue"))
                {
                    var insertValue = field.ExtendedMetadata["InsertValue"].ToString();
                    if (insertValue == "@now")
                    {
                        fieldValues.Add("GETDATE()");
                    }
                    else if (insertValue == "@by")
                    {
                        fieldValues.Add("COALESCE(SUSER_SNAME(), 'system')");
                    }
                    else
                    {
                        fieldValues.Add(insertValue);
                    }
                }
                else
                {
                    fieldValues.Add($"i.[{field.BaseField.Name}]");
                }
            }

            sb.AppendLine(string.Join(", ", fieldValues));
            sb.AppendLine("    FROM INSERTED i;");
            sb.AppendLine("END");
            sb.AppendLine("GO");
            sb.AppendLine();
        }
    }

    private void GenerateUpdateTriggers(StringBuilder sb, MDDModel model)
    {
        // Check if any field has update values
        var fieldsWithUpdateValues = model.Fields
            .Where(f => f.ExtendedMetadata.ContainsKey("UpdateValue"))
            .ToList();

        if (fieldsWithUpdateValues.Any())
        {
            sb.AppendLine($"CREATE TRIGGER [TR_{model.BaseModel.Name}_Update]");
            sb.AppendLine($"ON [{_document.BaseDocument.Namespace}].[{model.BaseModel.Name}]");
            sb.AppendLine("INSTEAD OF UPDATE");
            sb.AppendLine("AS");
            sb.AppendLine("BEGIN");
            sb.AppendLine("    SET NOCOUNT ON;");
            sb.AppendLine();

            // Build update statement
            sb.AppendLine($"    UPDATE t SET");

            // Map fields with their update values
            var updateStatements = new List<string>();
            foreach (var field in model.Fields)
            {
                if (field.ExtendedMetadata.ContainsKey("UpdateValue"))
                {
                    var updateValue = field.ExtendedMetadata["UpdateValue"].ToString();
                    if (updateValue == "@now")
                    {
                        updateStatements.Add($"        t.[{field.BaseField.Name}] = GETDATE()");
                    }
                    else if (updateValue == "@by")
                    {
                        updateStatements.Add($"        t.[{field.BaseField.Name}] = COALESCE(SUSER_SNAME(), 'system')");
                    }
                    else
                    {
                        updateStatements.Add($"        t.[{field.BaseField.Name}] = {updateValue}");
                    }
                }
                else
                {
                    updateStatements.Add($"        t.[{field.BaseField.Name}] = i.[{field.BaseField.Name}]");
                }
            }

            sb.AppendLine(string.Join(",\n", updateStatements));
            sb.AppendLine($"    FROM [{_document.BaseDocument.Namespace}].[{model.BaseModel.Name}] t");
            sb.AppendLine("    INNER JOIN INSERTED i ON t._id = i._id;");
            sb.AppendLine("END");
            sb.AppendLine("GO");
            sb.AppendLine();
        }
    }

    private List<MDDField> GetAllFields(MDDModel model)
    {
        var fields = new List<MDDField>(model.Fields);

        // Add fields from inherited models and interfaces
        foreach (var inheritedTypeName in model.BaseModel.Inherits)
        {
            var inheritedModel = _document.Models.FirstOrDefault(m => m.BaseModel.Name == inheritedTypeName);
            if (inheritedModel != null)
            {
                fields.AddRange(GetAllFields(inheritedModel));
                continue;
            }

            var inheritedInterface = _document.Interfaces.FirstOrDefault(i => i.BaseInterface.Name == inheritedTypeName);
            if (inheritedInterface != null)
            {
                fields.AddRange(inheritedInterface.Fields);
            }
        }

        return fields;
    }
}