using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Units;
using System.Text;

namespace MDDBooster.Builders
{
    internal class TsModelBuilder
    {
        private static readonly Dictionary<string, string> typeMap = new()
        {
            { "bool", "boolean" },
            { "int", "number" },
            { "float", "number" },
            { "double", "number" },
            { "decimal", "number" },
            { "Guid", "string" },
            { "JsonElement", "object" },
            { "Stream", "Blob" },
            { "byte[]", "Blob" },
        };

#pragma warning disable IDE1006 // Naming Styles
        private record StoreRecord(string ns, string modelPath, string tsFile,
            IEnumerable<EnumUnit> enumTypes,
            IEnumerable<InterfaceUnit> interfaces,
            IEnumerable<ClassUnit> abstracts,
            IEnumerable<ClassUnit> classes);
#pragma warning restore IDE1006 // Naming Styles

        private static readonly List<StoreRecord> store = new();

        private IEnumerable<InterfaceUnit> interfaces = Enumerable.Empty<InterfaceUnit>();
        private IEnumerable<ClassUnit> abstracts = Enumerable.Empty<ClassUnit>();
        private IEnumerable<ClassUnit> classes = Enumerable.Empty<ClassUnit>();

        internal async Task BuildAsync(string ns, string modelPath, string tsFile)
        {
            var handlers = new List<CsCodeUnit>();
            foreach (var file in Directory.GetFiles(modelPath))
            {
                var code = await File.ReadAllTextAsync(file);
                var csHandler = new CsCodeUnit(code);
                handlers.Add(csHandler);
            }

            var usingLines = new List<string>();
            var sb = new StringBuilder();

            var enumTypes = handlers.SelectMany(handler => handler.EnumUnits);

            foreach (var enumType in enumTypes)
            {
                sb.AppendLine($"  export enum {enumType.Name} {{");

                var lines = enumType.GetValueNames().Select(valueName => $"    {valueName} = '{valueName}'");
                sb.Append(string.Join("," + Constants.NewLine, lines));
                sb.AppendLine();
                sb.AppendLine($"  }}");
                sb.AppendLine();
            }

            this.interfaces = handlers.SelectMany(handler => handler.InterfaceUnits);
            this.abstracts = handlers.SelectMany(handler => handler.AbstractUnits);
            this.classes = handlers.SelectMany(handler => handler.ClassUnits);

            foreach (var model in interfaces)
            {
                WriteClass(model, sb, usingLines, isInterface: true);
            }

            foreach (var model in abstracts)
            {
                WriteClass(model, sb, usingLines, isAbstracts: true);
            }

            foreach (var model in classes)
            {
                WriteClass(model, sb, usingLines);
            }

            var usingLinesText = string.Join(Constants.NewLine, usingLines);
            if (usingLinesText.Length > 0)
            {
                usingLinesText += Constants.NewLine + Constants.NewLine;
            }
            var output = $@"{usingLinesText}export namespace {ns} {{

{sb}}}";
            await File.WriteAllTextAsync(tsFile, output);

            store.Add(new StoreRecord(ns, modelPath, tsFile, enumTypes, interfaces, abstracts, classes));
        }

        private void WriteClass(IClassTypeUnit model, StringBuilder sb, List<string> usingLines, bool isInterface = false, bool isAbstracts = false)
        {
            var className = model.Name;

            var extends = string.Join(",", model.Inherits);
            if (string.IsNullOrEmpty(extends) != true)
            {
                var extendsLine = ResolveTypeNames(extends, usingLines);
                extends = extendsLine.Length > 0 ? $" extends {extendsLine}" : null;
            }

            if (model is ClassUnit classUnit && classUnit.IsGeneric)
            {
                sb.AppendLine($"  // @ts-ignore");
                className += $"<{string.Join(",", classUnit.GenericTypeNames)}>";
            }

            if (isInterface)
            {
                sb.AppendLine($"  export interface {className}{extends} {{");
            }
            else if (isAbstracts)
            {
                sb.AppendLine($"  export abstract class {className}{extends} {{");
            }
            else
            {
                sb.AppendLine($"  export class {className}{extends} {{");
            }

            foreach (var property in model.Properties)
            {
                var comment = string.Empty;
                var isNullable = false;
                var type = property.Type;

                if (property.IsRequired) // required
                {
                    isNullable = false;
                }
                if (property.IsNullable) // ?
                {
                    isNullable = true;
                }
                else if (isNullable)
                {
                    isNullable = false;
                }
                
                string tsType;
                if (typeMap.ContainsKey(type))
                {
                    tsType = typeMap[type];
                    comment += $" // {type}";
                }
                else
                {
                    tsType = type;
                }
                string typeName;
                if (tsType.StartsWith("IEnumerable"))
                {
                    typeName = tsType.GetBetween("<", ">");

                    if (typeMap.ContainsKey(typeName))
                    {
                        comment += $" // {typeName}";
                        typeName = typeMap[typeName];
                    }
                    var m = FindModel(typeName);
                    if (m.Item1 != null)
                    {
                        typeName = $"{m.Item1.ns}.{typeName}";
                        TryAddUsingLInes(m, usingLines);
                    }

                    tsType = string.Concat("Array", "<", typeName, ">");
                    isNullable = true;
                }
                else
                {
                    typeName = tsType;
                    var m = FindModel(tsType);
                    if (m.Item1 != null)
                    {
                        tsType = $"{m.Item1.ns}.{typeName}";
                        TryAddUsingLInes(m, usingLines);
                    }
                }

                var name = property.Name.ToCamel();
                var nullable = isNullable
                    ? "?"
                    : isInterface
                    ? ""
                    : "!";
                sb.AppendLine($"    {name}{nullable}: {tsType};{comment}");
            }

            // replace
            sb.Replace("IEnumerable<", "Array<");

            sb.AppendLine($"  }}");
            sb.AppendLine();
        }

        public static IEnumerable<string> GetTypeHierarchy(string input)
        {
            return input.Split("<").Select(p => p.Replace(">", string.Empty));
        }

        private string ResolveTypeNames(string typeLine, List<string> usingLines)
        {
            if (typeLine.Contains('<')) // is generic
            {
                var baseName = typeLine.Left("<").Trim();
                var parameters = typeLine.GetBetweenBlock("<", ">").Split(",").Select(p => p.Trim());

                var sb = new StringBuilder();
                var resolved = ResolveTypeNames(baseName, usingLines);
                if (string.IsNullOrEmpty(resolved) != true) sb.Append(resolved);

                var list = new List<string>();
                foreach (var parameter in parameters)
                {
                    resolved = ResolveTypeNames(parameter, usingLines);
                    if (string.IsNullOrEmpty(resolved) != true) list.Add(resolved);
                }
                if (list.Count > 0)
                {
                    sb.Append('<');
                    sb.Append(string.Join(",", list));
                    sb.Append('>');
                }

                return sb.ToString();
            }

            var typeName = typeLine.Trim();
            if (typeName.Equals("IEnumerable")) return "Array";

            if (this.interfaces.Any(p => p.Name == typeName)
                || this.abstracts.Any(p => p.Name == typeName)
                || this.classes.Any(p => p.Name == typeName))
            {
                // 같은 파일에 있어서 using을 추가할 필요가 없습니다.
                // 아무것도 하지 않음
            }
            else
            {
                var m = FindModel(typeName);
                if (m.Item1 == null)
                {
                    return string.Empty;
                }
                else
                {
                    typeName = $"{m.Item1.ns}.{typeName}";
                    TryAddUsingLInes(m, usingLines);
                }
            }

            return typeName;
        }

        private static void TryAddUsingLInes((StoreRecord, List<string>) m, List<string> usingLines)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(m.Item1.tsFile);
            var line = $"import {{ {m.Item1.ns} }} from './{fileName}';";
            if (usingLines.Contains(line)) return;

            usingLines.Add(line);
        }

        private static ValueTuple<StoreRecord, List<string>> FindModel(string name)
        {
            foreach(var s in store)
            {
                if (s.enumTypes.FirstOrDefault(p => p.Name == name) is EnumUnit @enum)
                    return (s, @enum.GetValueNames().ToList());

                else if (s.interfaces.FirstOrDefault(p => p.Name == name) is InterfaceUnit @interface)
                    return (s, @interface.GetPropertyNames().ToList());

                else if (s.abstracts.FirstOrDefault(p => p.Name == name) is ClassUnit @abstract)
                    return (s, @abstract.GetPropertyNames().ToList());

                else if (s.classes.FirstOrDefault(p => p.Name == name) is ClassUnit @class)
                    return (s, @class.GetPropertyNames().ToList());
            }
            return default;
        }
    }
}
