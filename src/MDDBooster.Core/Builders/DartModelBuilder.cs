
using Microsoft.CodeAnalysis.CSharp.Units;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Formats.Tar;
using System.IO;
using System.Text;

namespace MDDBooster.Builders
{
    internal class DartModelBuilder
    {
        private static string[] skipNamesInForNew = ["_id", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"];
        private static readonly Dictionary<string, string> typeMap = new()
        {
            { "string", "String" },
            { "Guid", "String" },
            { "decimal", "double" }
        };

        private async Task WriteDartFileAsync(string code, string dartFile)
        {
            var dartContent = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}

{code}";
            var dir = Path.GetDirectoryName(dartFile)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(dartFile, dartContent);
        }

        internal async Task BuildAsync(string csFile, string dartFile)
        {
            var code = await File.ReadAllTextAsync(csFile);
            var csHandler = new CsCodeUnit(code);

            var sb = new StringBuilder();
            foreach (var classUnit in csHandler.ClassUnits)
            {
                var lines = BuildDartClass(classUnit, csHandler);
                sb.AppendLine(lines);
            }

            await WriteDartFileAsync(sb.ToString(), dartFile);
        }

        internal async Task BuildAsync(IModelMeta[] models, string output)
        {   
            foreach(var model in models)
            {
                if (model is TableMeta table)
                {
                    var code = BuildDartClass(table)!;
                    var fileName = table.Name.ToSnakeCase();

                    var dartFile = Path.Combine(output, $"{fileName}.dart");
                    await WriteDartFileAsync(code, dartFile);
                }
            }
        }

        private string? BuildDartClass(TableMeta table)
        {
            var properties = table.FullColumns.Select(c => new CsProperty
            {
                Type = c.GetSystemTypeAlias(),
                Name = c.Name,
                IsNullable = c.IsNullable,
            }).ToArray();

            return BuildDartClass(table.Name, properties);
        }

        private string? BuildDartClass(ClassUnit classUnit, CsCodeUnit csHandler)
        {
            return BuildDartClass(classUnit.Name, classUnit.Properties.Select(p => new CsProperty
            {
                Type = p.Type,
                Name = p.Name,
                IsEnumerable = p.IsEnumerable,
                GenericType = p.GenericType,
                IsNullable = p.IsNullable,
            }).ToArray());
        }

        private string? BuildDartClass(string className, CsProperty[] properties)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"class {className} {{");
            foreach(var property in properties)
            {
                var type = AsDartType(property);
                var name = AsDartName(property.Name);
                var nullable = property.IsNullable ? "?" : "";
                sb.AppendLine($"  {type}{nullable} {name};");
            }

            // 생성자
            sb.AppendLine();
            sb.AppendLine($"  {className}({{");
            foreach(var property in properties)
            {
                var type = AsDartType(property);
                var name = AsDartName(property.Name);
                var nullable = property.IsNullable ? "?" : "";
                var required = property.IsNullable ? "" : "required ";
                sb.AppendLine($"    {required}this.{name},");
            }
            sb.AppendLine("  });");

            // factory fromJson
            sb.AppendLine();
            sb.AppendLine($"  factory {className}.fromJson(Map<String, dynamic> json) => {className}(");
            foreach(var property in properties)
            {
                var type = AsDartType(property);
                var name = AsDartName(property.Name);
                var pName = property.Name.ToCamel(false);
                var nullable = property.IsNullable ? "?" : "";
                var value = $"json['{pName}']";
                if (type == "DateTime")
                {
                    if (property.IsNullable)
                        value = $"{value} != null ? DateTime.parse({value}) : null";
                    else
                        value = $"DateTime.parse({value})";
                }

                if (property.IsEnumerable && property.IsNullable)
                {
                    value = $"json['{pName}'] != null ? {type}.from(json['{pName}']) : null";
                }

                sb.AppendLine($"    {name}: {value},");
            }
            sb.AppendLine("  );");

            // factory forNew
            sb.AppendLine();
            sb.AppendLine($"  factory {className}.forNew({{");
            foreach(var property in properties)
            {
                if (skipNamesInForNew.Contains(property.Name)) continue;

                var type = AsDartType(property);
                var name = AsDartName(property.Name);
                var required = property.IsNullable ? "" : "required ";
                var nullable = property.IsNullable ? "?" : "";
                sb.AppendLine($"    {required}{type}{nullable} {name},");
            }
            sb.AppendLine("  }) {");

            sb.AppendLine($"    return {className}(");
            foreach (var property in properties)
            {
                var name = AsDartName(property.Name);
                var required = property.IsNullable ? "" : "required ";
                var type = typeMap.TryGetValue(property.Type, out string? value) ? value : property.Type;
                var nullable = property.IsNullable ? "?" : "";

                if (skipNamesInForNew.Contains(property.Name))
                {
                    if (property.IsNullable) continue;
                    value = property.Type switch
                    {
                        "string" => "''",
                        "Guid" => "''",
                        "decimal" => "0",
                        "DateTime" => "DateTime.now()",
                        _ => "null",
                    };
                    sb.AppendLine($"      {name}: {value},");
                }
                else
                {
                    sb.AppendLine($"      {name}: {name},");
                }
                //return Location(
                //    name: name,
                //    countryCode: countryCode,
                //    country: country,
                //    city: city,
                //    district: district,
                //    street: street,
                //    latitude: latitude,
                //    longitude: longitude,
                //    createdAt: DateTime.now(),
                //    createdBy: '',
                //    id: '');
            }
            sb.AppendLine("    );");
            sb.AppendLine("  }");

            // toJson
            sb.AppendLine();
            sb.AppendLine($"  Map<String, dynamic> toJson() => {{");
            foreach(var property in properties)
            {
                var type = property.Type;
                var name = AsDartName(property.Name);
                var pName = property.Name.ToCamel(false);
                var nullable = property.IsNullable ? "?" : "";

                var value = name;
                if (type == "DateTime")
                {
                    value = $"{name}{nullable}.toIso8601String()";
                }
                sb.AppendLine($"    '{pName}': {value},");
            }
            sb.AppendLine("  };");

            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string AsDartType(CsProperty property)
        {
            var type = typeMap.TryGetValue(property.Type, out string? value) ? value : property.Type;
            if (property.IsEnumerable)
            {
                type = property.GenericType;
                if (type != null && typeMap.TryGetValue(type, out string? mapType)) type = mapType;
                type = $"List<{type}>";
            }
            return type;
        }

        private static string AsDartName(string name)
        {
            if (name.StartsWith('_')) 
                return name[1..].ToCamelWithoutUnderline();
            else
                return name.ToCamelWithoutUnderline();
        }
    }

    public class CsProperty
    {
        public required string Type { get; set; }
        public required string Name { get; set; }
        public bool IsEnumerable { get; set; }
        public string? GenericType { get; set; }
        public bool IsNullable { get; set; }
    }
}