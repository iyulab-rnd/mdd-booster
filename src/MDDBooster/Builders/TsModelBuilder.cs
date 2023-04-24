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
        private record StoreRecord(string ns, string modelPath, string tsFile, Dictionary<string, List<string>> enumTypes, Dictionary<string, List<string>> abstracts, Dictionary<string, List<string>> models);
#pragma warning restore IDE1006 // 명명 스타일
        private static readonly List<StoreRecord> store = new();

        private Dictionary<string, List<string>> enumTypes = new();
        private Dictionary<string, List<string>> abstracts = new();
        private Dictionary<string, List<string>> models = new();

        internal async Task BuildAsync(string ns, string modelPath, string tsFile)
        {
            List<string>? list = null;
            foreach (var file in Directory.GetFiles(modelPath))
            {
                var text = await File.ReadAllTextAsync(file);
                var enumTypesOn = false;
                foreach (var line in text.Split(Environment.NewLine))
                {
                    var trimLine = line.LeftOr("//").Trim();
                    if (trimLine.Length < 1) continue;

                    if (trimLine.Contains("public abstract class "))
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
                    else if (trimLine.Contains("public enum "))
                    {
                        enumTypesOn = true;
                        var name = trimLine.Right("public enum ").Trim();
                        list = new();
                        enumTypes.Add(name, list);
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

                foreach (var line in enumType.Value)
                {
                    var trimLine = line.LeftOr("//").Trim();
                    if (trimLine.Length < 1) continue;

                    sb.AppendLine($"    {trimLine}");
                }

                sb.AppendLine($"  }}");
                sb.AppendLine();
            }

            foreach (var model in abstracts)
            {
                WriteClass(model, sb, isAbstracts: true, usingLines);
            }

            foreach (var model in models)
            {
                WriteClass(model, sb, isAbstracts: false, usingLines);
            }

            var usingLinesText = string.Join(Environment.NewLine, usingLines);
            if (usingLinesText.Length > 0)
            {
                usingLinesText += Environment.NewLine + Environment.NewLine;
            }
            var output = $@"{usingLinesText}export namespace {ns} {{

{sb}}}";
            await File.WriteAllTextAsync(tsFile, output);

            store.Add(new StoreRecord(ns, modelPath, tsFile, enumTypes, abstracts, models));
        }

        private void WriteClass(KeyValuePair<string, List<string>> model, StringBuilder sb, bool isAbstracts, List<string> usingLines)
        {
            var className = model.Key.LeftOr(":").Trim();
            var extends = model.Key.Right(":", false, false).Trim();
            if (string.IsNullOrEmpty(extends) != true)
            {
                var n = extends.LeftOr("<");
                var find = this.abstracts.Keys.FirstOrDefault(p => p.LeftOr("<") == n)
                    ?? this.models.Keys.FirstOrDefault(p => p.LeftOr("<") == n);

                if (find == null)
                {
                    var m = FindModel(extends);
                    if (m.Item1 == null)
                    {
                        extends = null;
                    }
                    else
                    {
                        extends = $" extends {m.Item1.ns}.{extends}";
                        TryAddUsingLInes(m, usingLines);
                    }
                }
                else
                {
                    extends = $" extends {extends}";
                }
            }
            sb.AppendLine($"  export {(isAbstracts ? "abstract " : "")}class {className}{extends} {{");

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
                
                else if (s.abstracts.ContainsKey(name))
                    return (s, s.abstracts[name]);

                else if (s.models.ContainsKey(name))
                    return (s, s.models[name]);
            }
            return default;
        }
    }
}
