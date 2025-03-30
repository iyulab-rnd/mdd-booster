using MDDBooster.Models;

namespace MDDBooster.Builders.MsSql;

public static class MsSqlExtensions
{
    /// <summary>
    /// Extension method to check if a field should be excluded from SQL generation
    /// </summary>
    public static bool ShouldExcludeFromSql(this MDDField field)
    {
        return field.ExtendedMetadata.ContainsKey("ExcludeFromGeneration") &&
               (bool)field.ExtendedMetadata["ExcludeFromGeneration"];
    }

    /// <summary>
    /// Extension method to generate SQL for a field
    /// </summary>
    public static string ToSqlColumnDefinition(this MDDField field, string sqlType)
    {
        var nullableStr = field.BaseField.IsNullable ? "NULL" : "NOT NULL";
        var defaultStr = !string.IsNullOrEmpty(field.BaseField.DefaultValue)
            ? $" DEFAULT {TransformDefaultValue(field.BaseField.DefaultValue, field.BaseField.Type)}"
            : "";

        var result = $"[{field.BaseField.Name}] {sqlType} {nullableStr}{defaultStr}";

        // Check for special attributes
        if (field.BaseField.IsPrimaryKey)
        {
            result += " PRIMARY KEY";
        }

        return result;
    }

    /// <summary>
    /// Extension method to get SQL triggers for a specific field
    /// </summary>
    public static string GetSqlTriggers(this MDDField field, string tableName, string schema)
    {
        var sb = new StringBuilder();

        // Generate trigger for insert value
        if (field.ExtendedMetadata.ContainsKey("InsertValue"))
        {
            var insertValue = field.ExtendedMetadata["InsertValue"].ToString();
            string sqlValue;

            if (insertValue == "@now")
            {
                sqlValue = "GETDATE()";
            }
            else if (insertValue == "@by")
            {
                sqlValue = "COALESCE(SUSER_SNAME(), 'system')";
            }
            else
            {
                sqlValue = insertValue;
            }

            sb.AppendLine($"-- Trigger for inserting {field.BaseField.Name}");
            // Trigger definition would go here
        }

        // Generate trigger for update value
        if (field.ExtendedMetadata.ContainsKey("UpdateValue"))
        {
            var updateValue = field.ExtendedMetadata["UpdateValue"].ToString();
            string sqlValue;

            if (updateValue == "@now")
            {
                sqlValue = "GETDATE()";
            }
            else if (updateValue == "@by")
            {
                sqlValue = "COALESCE(SUSER_SNAME(), 'system')";
            }
            else
            {
                sqlValue = updateValue;
            }

            sb.AppendLine($"-- Trigger for updating {field.BaseField.Name}");
            // Trigger definition would go here
        }

        return sb.ToString();
    }

    private static string TransformDefaultValue(string defaultValue, string type)
    {
        // Handle special default values
        if (defaultValue == "now()")
        {
            return "GETDATE()";
        }
        else if (defaultValue == "true")
        {
            return "1";
        }
        else if (defaultValue == "false")
        {
            return "0";
        }
        else if (defaultValue.StartsWith("\"") && defaultValue.EndsWith("\""))
        {
            // String literal
            return $"N{defaultValue}";
        }
        else if (defaultValue == "@now")
        {
            return "GETDATE()";
        }
        else if (defaultValue == "@by")
        {
            return "N'system'";
        }

        // Default handling
        switch (type.ToLowerInvariant())
        {
            case "string":
            case "text":
                return $"N'{defaultValue}'";
            default:
                return defaultValue;
        }
    }
}