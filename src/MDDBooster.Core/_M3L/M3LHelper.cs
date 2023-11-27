using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace M3L
{
    public static class M3LHelper
    {
        private static readonly Dictionary<Type, string> typeTotypeAliasMap = new()
        {
            { typeof(bool), "bool" },
            { typeof(string), "string" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(float), "float" },
            { typeof(DateTime), "DateTime" },
            { typeof(Guid), "Guid" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(char), "char" },
            { typeof(byte), "byte" },
            { typeof(byte[]), "byte[]" }
        };

        private static string SystemTypeToTypeText(Type type)
        {
            if (typeTotypeAliasMap.TryGetValue(type, out var text))
                return text;

            else
                return type.Name;
        }

        private static string Serialize(Entity entity)
        {
#if DEBUG
            if (entity.Name == "StaticFile")
            {
            }
#endif
            var sb = new StringBuilder();
            var baseText = entity.BaseEntity;
            var interfacesText = entity.Interfaces == null ? string.Empty : string.Join(", ", entity.Interfaces);
            baseText = string.IsNullOrEmpty(interfacesText) ? baseText : baseText + ", " + interfacesText;

            var baseLine = string.Empty;
            if (string.IsNullOrEmpty(baseText) != true) baseLine = ": " + baseText;

            sb.AppendLine($"## {entity.Name}{baseLine}");

            if (entity.Properties != null)
            {
                foreach (var property in entity.Properties)
                {
                    var optional = property.IsNotNull ? string.Empty : "?";
                    var typeText = SystemTypeToTypeText(property.SystemType ?? throw new Exception("Null SystemType"));
                    var label = string.IsNullOrEmpty(property.Label) ? string.Empty : $"({property.Label})";

                    var sizeText = string.Empty;
                    if (property.Length > 0)
                    {
                        sizeText = $"({property.Length})";
                    }

                    var defaultText = string.Empty;
                    if (string.IsNullOrEmpty(property.DefaultValue) != true)
                    {
                        defaultText = $" = {property.DefaultValue}";
                    }

                    var attrText = string.Empty;
                    if (property.IsUnique) attrText += "[UQ]";
                    if (property.UseIndex) attrText += "[UI]";
                    if (property.ForeignKey != null)
                    {
                        var fk = property.ForeignKey;
                        if (fk.Entity is null)
                            attrText += "[FK]";

                        else if (fk.Property is null)
                            attrText += $"[FK: {fk.Entity}]";

                        else 
                            attrText += $"[FK: {fk.Entity}.{fk.Property}]";
                    }

                    if (string.IsNullOrEmpty(attrText) != true)
                    {
                        attrText = "\t\t" + attrText;
                    }

                    var propertyLine = $"- {property.Name}{label}: {typeText}{optional}{sizeText}{defaultText}{attrText}";
                    sb.AppendLine(propertyLine);
                }
            }

            return sb.ToString();
        }

        public static string Serialize(IEnumerable<Entity> entities)
        {
            var text = string.Join(Environment.NewLine, entities.Select(Serialize));
            return text;
        }
    }
}
