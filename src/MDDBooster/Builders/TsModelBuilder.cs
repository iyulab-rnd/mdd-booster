using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MDDBooster.Builders
{
    internal class TsModelBuilder
    {
        private static readonly Dictionary<string, string> typeMap = new()
        {
            { "Guid", "string" },
            { "JsonElement", "string" },
            { "bool", "boolean" },
            { "int", "number" }
        };

#pragma warning disable IDE1006 // 명명 스타일
        private record StoreRecord(string ns, string modelPath, string tsFile, 
            Dictionary<string, List<string>> enumTypes,
            Dictionary<string, List<string>> interfaces,
            Dictionary<string, List<string>> abstracts, 
            Dictionary<string, List<string>> models);
#pragma warning restore IDE1006 // 명명 스타일
        private static readonly List<StoreRecord> store = new();

        private readonly Dictionary<string, List<string>> enumTypes = new();
        private readonly Dictionary<string, List<string>> interfaces = new();
        private readonly Dictionary<string, List<string>> abstracts = new();
        private readonly Dictionary<string, List<string>> models = new();

        internal async Task BuildAsync(string ns, string modelPath, string tsFile)
        {
            List<string>? list = null;
            foreach (var file in Directory.GetFiles(modelPath))
            {
                var text = await File.ReadAllTextAsync(file);
                var enumTypesOn = false;
                foreach (var line in text.Split(Environment.NewLine))
                {
                    var trimLine = Functions.GetConentLine(line);
                    if (trimLine.Length < 1) continue;

                    if (trimLine.Contains("public enum "))
                    {
                        enumTypesOn = true;
                        var name = trimLine.Right("public enum ").Trim();
                        list = new();
                        enumTypes.Add(name, list);
                    }
                    else if (trimLine.Contains("public interface "))
                    {
                        var name = trimLine.Right("public interface ").Trim();
                        list = new();
                        interfaces.Add(name, list);
                    }
                    else if (trimLine.Contains("public abstract class "))
                    {
                        var name = trimLine.Right("public abstract class ").Trim();
                        list = new();
                        abstracts.Add(name, list);
                    }
                    else if (trimLine.Contains("public class "))
                    {
                        var name = trimLine.Right("public class ").Trim();
                        list = new();
                        models.Add(name, list);
                    }
                    else if (trimLine.Contains("public ") && trimLine.Contains("get;") && trimLine.Contains("set;"))
                    {
                        list?.Add(trimLine.Trim());
                    }
                    else if (enumTypesOn)
                    {
                        if (trimLine.StartsWith("{"))
                        {
                            // skip
                        }
                        else if (trimLine.StartsWith("}"))
                        {
                            enumTypesOn = false;
                        }
                        else
                        {
                            list?.Add(trimLine);
                        }
                    }
                }
            }

            var usingLines = new List<string>();
            var sb = new StringBuilder();

            foreach (var enumType in enumTypes)
            {
                sb.AppendLine($"  export enum {enumType.Key} {{");

                var first = true;
                foreach (var line in enumType.Value)
                {
                    var name = line.Left(",").Trim();
                    if (string.IsNullOrEmpty(name)) continue;

                    if (first)
                        first = false;
                    else
                    {
                        sb.AppendLine(",");
                    }
                    sb.Append($"    {name} = '{name}'");
                }
                sb.AppendLine();
                sb.AppendLine($"  }}");
                sb.AppendLine();
            }

            foreach (var model in interfaces)
            {
                WriteClass(model, sb, usingLines, isInterface: true);
            }

            foreach (var model in abstracts)
            {
                WriteClass(model, sb, usingLines, isAbstracts: true);
            }

            foreach (var model in models)
            {
                WriteClass(model, sb, usingLines);
            }

            var usingLinesText = string.Join(Environment.NewLine, usingLines);
            if (usingLinesText.Length > 0)
            {
                usingLinesText += Environment.NewLine + Environment.NewLine;
            }
            var output = $@"{usingLinesText}export namespace {ns} {{

{sb}}}";
            await File.WriteAllTextAsync(tsFile, output);

            store.Add(new StoreRecord(ns, modelPath, tsFile, enumTypes, interfaces, abstracts, models));
        }

        private void WriteClass(KeyValuePair<string, List<string>> model, StringBuilder sb, List<string> usingLines, bool isInterface = false, bool isAbstracts = false)
        {
            var className = model.Key.LeftOr(":").Trim();
            var extends = model.Key.Right(":", false, false).Trim();
            if (string.IsNullOrEmpty(extends) != true)
            {
                var extendsLine = ResolveTypeNames(extends, usingLines);
                extends = extendsLine.Length > 0 ? $" extends {extendsLine}" : null;
            }

            if (className.Contains('<') && className.Contains('>'))
            {
                sb.AppendLine($"  // @ts-ignore");
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

            foreach (var line in model.Value)
            {
                var comment = string.Empty;
                var isNullable = true;
                var t = line.Right("public ").Trim();
                if (t.StartsWith("required "))
                {
                    t = line.Right("required ").Trim();
                    isNullable = false;
                }

                var type = t.Left(" ");
                if (type.EndsWith("?"))
                {
                    isNullable = true;
                    type = type[..^1];
                }
                if (type == "Guid") comment += " // Guid";
                else if (type == "JsonElement") comment += " // JsonElement";
                var tsType = typeMap.ContainsKey(type) ? typeMap[type] : type;
                string typeName;
                if (tsType.StartsWith("IEnumerable"))
                {
                    typeName = tsType.GetBetween("<", ">");
                    if (typeName == "Guid") comment += " // Guid";
                    else if (typeName == "JsonElement") comment += " // JsonElement";
                    typeName = typeMap.ContainsKey(typeName) ? typeMap[typeName] : typeName;

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
                t = t.Right(" ", false, false);
                var name = t.Left(" ").ToCamel();
                var nullable = isNullable ? "?" : "!";
                sb.AppendLine($"    {name}{nullable}: {tsType};{comment}");
            }

            sb.AppendLine($"  }}");
            sb.AppendLine();
        }

        public static IEnumerable<string> GetTypeHierarchy(string input)
        {
            return input.Split("<").Select(p => p.Replace(">", string.Empty));
        }

        private string ResolveTypeNames(string nameText, List<string> usingLines)
        {
            var sb = new StringBuilder();

            var names = GetTypeHierarchy(nameText).ToArray();
            var n = 0;
            for (int i = 0; i < names.Length; i++)
            {
                bool isGeneric = i > 0;
                var name = names[i];

                var find = this.interfaces.Keys.FirstOrDefault(p => p.LeftOr("<").LeftOr(" ") == name)
                    ?? this.abstracts.Keys.FirstOrDefault(p => p.LeftOr("<").LeftOr(" ") == name)
                    ?? this.models.Keys.FirstOrDefault(p => p.LeftOr("<").LeftOr(" ") == name);


                string s;
                if (find == null)
                {
                    // 예약어
                    if (name == "IEnumerable")
                        name = "Array";

                    else if (name == "object")
                        name = "Object";

                    else
                    {
                        // 외부에서 찾음
                        var m = FindModel(name);
                        if (m.Item1 == null)
                        {
                            // 찾을수 없음.. 생략
                            continue;
                        }
                        else
                        {
                            name = $"{m.Item1.ns}.{name}";
                            TryAddUsingLInes(m, usingLines);
                        }
                    }
                }

                if (isGeneric)
                    s = $"<{name}";
                else
                    s = name;

                sb.Append(s);
                n++;
            }

            if (n > 1)
            {
                sb.Append('>', n - 1);
            }

            return sb.ToString();
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
                if (s.enumTypes.ContainsKey(name))
                    return (s, s.enumTypes[name]);

                else if (s.interfaces.ContainsKey(name))
                    return (s, s.interfaces[name]);

                else if (s.abstracts.ContainsKey(name))
                    return (s, s.abstracts[name]);

                else if (s.models.ContainsKey(name))
                    return (s, s.models[name]);
            }
            return default;
        }
    }
}
