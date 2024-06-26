
using Microsoft.CodeAnalysis.CSharp.Units;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MDDBooster.Builders
{
    internal class DartModelBuilder
    {
        private static readonly Dictionary<string, string> typeMap = new()
        {
            { "string", "String" },
            { "Guid", "String" },
        };

        internal async Task BuildAsync(string csFile, string dartFile)
        {
            var code = await File.ReadAllTextAsync(csFile);
            var csHandler = new CsCodeUnit(code);

            var sb = new StringBuilder();
            foreach(var classUnit in csHandler.ClassUnits)
            {
                var lines = BuildDartClass(classUnit, csHandler);
                sb.AppendLine(lines);
            }

            var dartContent = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}

{sb.ToString()}";
            var dir = Path.GetDirectoryName(dartFile)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(dartFile, dartContent);
        }

        private string? BuildDartClass(ClassUnit classUnit, CsCodeUnit csHandler)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"class {classUnit.Name} {{");
            foreach(var property in classUnit.Properties)
            {
                var type = typeMap.ContainsKey(property.Type) ? typeMap[property.Type] : property.Type;
                if (property.IsEnumerable)
                {
                    type = property.GenericType;
                    if (type != null && typeMap.TryGetValue(type, out string? value)) type = value;
                    type = $"List<{type}>";
                }
                var name = property.Name.ToCamel();
                var nullable = property.IsNullable ? "?" : "";
                sb.AppendLine($"  {type}{nullable} {name};");
            }

            // 생성자
            sb.AppendLine();
            sb.AppendLine($"  {classUnit.Name}({{");
            foreach(var property in classUnit.Properties)
            {
                var type = typeMap.ContainsKey(property.Type) ? typeMap[property.Type] : property.Type;
                if (property.IsEnumerable)
                {
                    type = property.GenericType;
                    if (type != null && typeMap.TryGetValue(type, out string? value)) type = value;
                    type = $"List<{type}>";
                }
                var name = property.Name.ToCamel();
                var nullable = property.IsNullable ? "?" : "";
                var required = property.IsNullable ? "" : "required ";
                sb.AppendLine($"    {required}this.{name},");
            }
            sb.AppendLine("  });");

            // fromJson
            sb.AppendLine();
            sb.AppendLine($"  factory {classUnit.Name}.fromJson(Map<String, dynamic> json) => {classUnit.Name}(");
            foreach(var property in classUnit.Properties)
            {
                var type = property.Type;
                if (property.IsEnumerable)
                {
                    type = property.GenericType;
                    if (type != null && typeMap.TryGetValue(type, out string? mapType)) type = mapType;
                    type = $"List<{type}>";
                }
                var name = property.Name.ToCamel();
                var nullable = property.IsNullable ? "?" : "";
                var value = $"json['{property.Name}']";
                if (type == "DateTime")
                {
                    value = $"DateTime.parse({value})";
                }

                if (property.IsEnumerable && property.IsNullable)
                {
                    value = $"json['{property.Name}'] != null ? {type}.from(json['{property.Name}']) : null";
                }

                sb.AppendLine($"    {name}: {value},");
            }
            sb.AppendLine("  );");

            // toJson
            sb.AppendLine();
            sb.AppendLine($"  Map<String, dynamic> toJson() => {{");
            foreach(var property in classUnit.Properties)
            {
                var type = property.Type;
                var name = property.Name.ToCamel();
                var nullable = property.IsNullable ? "?" : "";

                var value = name;
                if (type == "DateTime")
                {
                    value = $"{name}.toIso8601String()";
                }
                sb.AppendLine($"    '{property.Name}': {value},");
            }
            sb.AppendLine("  };");

            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}