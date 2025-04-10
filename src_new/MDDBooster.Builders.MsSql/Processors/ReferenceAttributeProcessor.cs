using MDDBooster.Models;
using MDDBooster.Processors;

namespace MDDBooster.Builders.MsSql.Processors;

/// <summary>
/// Processor for handling reference attributes and behaviors
/// </summary>
public class ReferenceAttributeProcessor : IModelProcessor
{
    public void Process(MDDDocument document)
    {
        foreach (var model in document.Models)
        {
            foreach (var field in model.Fields)
            {
                ProcessFieldReferences(field);
            }
        }

        foreach (var interface_ in document.Interfaces)
        {
            foreach (var field in interface_.Fields)
            {
                ProcessFieldReferences(field);
            }
        }

        AppLog.Information("Processed reference attributes in {ModelCount} models and {InterfaceCount} interfaces",
            document.Models.Count, document.Interfaces.Count);
    }

    private void ProcessFieldReferences(MDDField field)
    {
        // Process reference attributes
        if (field.BaseField.IsReference)
        {
            AppLog.Debug("Processing reference field: {FieldName} referencing {Target}",
                field.BaseField.Name, field.BaseField.ReferenceTarget);

            // Default reference behavior is CASCADE
            if (!field.ExtendedMetadata.ContainsKey("OnDelete"))
            {
                field.ExtendedMetadata["OnDelete"] = "CASCADE";
            }

            // Process custom framework attributes
            foreach (var attr in field.FrameworkAttributes)
            {
                if (attr.Name.Equals("OnDelete", StringComparison.OrdinalIgnoreCase))
                {
                    string onDeleteAction = attr.Parameters.FirstOrDefault() ?? "CASCADE";
                    field.ExtendedMetadata["OnDelete"] = onDeleteAction;
                    AppLog.Debug("Field {FieldName} has OnDelete action: {Action}",
                        field.BaseField.Name, onDeleteAction);
                }
                else if (attr.Name.Equals("ForeignKey", StringComparison.OrdinalIgnoreCase))
                {
                    string constraintName = attr.Parameters.FirstOrDefault();
                    if (!string.IsNullOrEmpty(constraintName))
                    {
                        field.ExtendedMetadata["ForeignKeyName"] = constraintName;
                        AppLog.Debug("Field {FieldName} has custom foreign key name: {Name}",
                            field.BaseField.Name, constraintName);
                    }
                }
            }
        }
    }
}