﻿using Microsoft.CodeAnalysis;
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
            { "DateTime", "Date" },
        };

        private static readonly Dictionary<string, string> propertyMetaTypeMap = new()
        {
            { "string", "text" },
            { "bool", "checkbox" },
            { "int", "number" },
            { "float", "number" },
            { "double", "number" },
            { "decimal", "number" },
            { "uniqueidentifier", "select" },
            { "DateTime", "datetime" },
        };

#pragma warning disable IDE1006 // Naming Styles
        private record StoreRecord(string ns, IEnumerable<string> modelFiles, string tsFile,
            IEnumerable<EnumUnit> enumTypes,
            IEnumerable<InterfaceUnit> interfaces,
            IEnumerable<ClassUnit> abstracts,
            IEnumerable<ClassUnit> classes);
#pragma warning restore IDE1006 // Naming Styles

        private static readonly List<StoreRecord> store = [];

        private IEnumerable<InterfaceUnit> interfaces = [];
        private IEnumerable<ClassUnit> abstracts = [];
        private IEnumerable<ClassUnit> classes = [];

        internal async Task BuildAsync(string ns, IEnumerable<string> modelFiles, string tsFile)
        {
            var handlers = new List<CsCodeUnit>();

            SetupDefaultFiles(handlers);

            foreach (var file in modelFiles)
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

            foreach (var model in SortDependency(interfaces))
            {
                WriteClass(model, sb, usingLines, isInterface: true);
            }

            foreach (var model in SortDependency(abstracts))
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
            var output = $@"// Code generated by ""MDD Booster""; DO NOT EDIT.

import {{ propertyMeta }} from ""@iyulab/u-components"";

{usingLinesText}export namespace {ns};

{sb}
";
            var dir = System.IO.Path.GetDirectoryName(tsFile)!;
            if (Directory.Exists(dir) != true)
            {
                Directory.CreateDirectory(dir);
            }
            await Functions.FileWriteAsync(tsFile, output);

            store.Add(new StoreRecord(ns, modelFiles, tsFile, enumTypes, interfaces, abstracts, classes));
        }

        private void SetupDefaultFiles(List<CsCodeUnit> handlers)
        {
            var code = @"
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Iyu.Entity;

public interface IEntity
{   
}

public interface IKeyEntity : IEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    [Without]
    Guid _key { get; set; }
}

public abstract class KeyEntity : IKeyEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    [Without]
    public Guid _key { get; set; }
}

public interface IGuidEntity : IEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    [Without]
    Guid _id { get; set; }
}

public abstract class GuidEntity : IGuidEntity
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Required]
    [Without]
    public Guid _id { get; set; }
}
";
            handlers.Add(new CsCodeUnit(code));
        }

        private static IEnumerable<InterfaceUnit> SortDependency(IEnumerable<InterfaceUnit> interfaces)
        {
            return interfaces;
        }


        private static List<ClassUnit> SortDependency(IEnumerable<ClassUnit> classes)
        {
            // 클래스 이름과 ClassUnit 객체를 매핑하는 사전을 생성합니다.
            var classDict = classes.ToDictionary(c => c.Name, c => c);

            // 정렬된 리스트와 방문 상태를 기록하는 사전을 준비합니다.
            var sorted = new List<ClassUnit>();
            var visited = new Dictionary<string, bool>();

            // 재귀적으로 종속성을 해결하고 정렬 리스트에 추가하는 메서드입니다.
            void Visit(ClassUnit classUnit)
            {
                // 이미 방문한 클래스는 건너뜁니다.
                if (visited.TryGetValue(classUnit.Name, out bool isVisited))
                {
                    if (isVisited) // 이미 정렬된 클래스는 무시합니다.
                        return;
                    else
                        throw new Exception("순환 종속성이 존재합니다."); // 순환 종속성 발견
                }

                visited[classUnit.Name] = false; // 방문 중 표시

                // 상속받는 클래스를 먼저 방문합니다.
                foreach (var inheritName in classUnit.Inherits)
                {
                    if (classDict.TryGetValue(inheritName, out var parentClass))
                    {
                        Visit(parentClass); // 재귀적 방문
                    }
                }

                visited[classUnit.Name] = true; // 방문 완료 표시
                sorted.Add(classUnit); // 정렬된 리스트에 추가
            }

            // 모든 클래스를 순회하며 종속성 해결을 시도합니다.
            foreach (var classUnit in classes)
            {
                if (!visited.ContainsKey(classUnit.Name))
                {
                    Visit(classUnit);
                }
            }

            return sorted;
        }

        private void WriteClass(IClassTypeUnit model, StringBuilder sb, List<string> usingLines, bool isInterface = false, bool isAbstracts = false)
        {
            var className = model.Name;

            var extends = string.Join(",", model.GetExtends().Select(p => ResolveTypeNames(p, usingLines)));
            var interfaces = string.Join(",", model.GetInterfaces().Select(p => ResolveTypeNames(p, usingLines)));

            var inherits = "";
            if (isInterface)
            {
                if (extends.Length > 0 || interfaces.Length > 0)
                {
                    inherits = $" extends {extends}{(extends.Length > 0 && interfaces.Length > 0 ? "," : "")}{interfaces}";
                }
            }
            else
            {
                if (extends.Length > 0)
                {
                    inherits = $" extends {extends}";
                }
                if (interfaces.Length > 0)
                {
                    inherits += $" implements {interfaces}";
                }
            }

            if (model is ClassUnit classUnit && classUnit.IsGeneric)
            {
                sb.AppendLine($"  // @ts-ignore");
                className += $"<{string.Join(",", classUnit.GenericTypeNames)}>";
            }

            if (isInterface)
            {
                sb.AppendLine($"  export interface {className}{inherits} {{");
            }
            else if (isAbstracts)
            {
                sb.AppendLine($"  export abstract class {className}{inherits} {{");
            }
            else
            {
                sb.AppendLine($"  export class {className}{inherits} {{");
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
                if (typeMap.TryGetValue(type, out string? value))
                {
                    tsType = value;
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

                    if (typeMap.TryGetValue(typeName, out string? cachedTypeName))
                    {
                        comment += $" // {typeName}";
                        typeName = cachedTypeName;
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

                var name = property.Name.ToCamel(removeLowdash: false);
                var nullable = isNullable
                    ? "?"
                    : isInterface
                    ? ""
                    : "!";

                var propertyMetaDecorator = "";

                if (isInterface != true)
                {
                    var columnTypeName = property.GetColumnTypeName();

                    if (propertyMetaTypeMap.TryGetValue(type, out string? propertyMetaType))
                    {
                        var pType = propertyMetaType;
                        if (property.Name.Contains("email", StringComparison.OrdinalIgnoreCase) && property.Type == "string") pType = "email";
                        else if (property.Name.Contains("password", StringComparison.OrdinalIgnoreCase) && property.Type == "string") pType = "password";

                        var sRequired = property.IsRequired ? ", required: true" : "";
                        propertyMetaDecorator = $@"@propertyMeta({{ type: ""{pType}"", label: '{property.GetLabel()}'{sRequired} }})";
                    }
                }
                if (string.IsNullOrEmpty(propertyMetaDecorator) != true)
                {
                    sb.AppendLine($"    {propertyMetaDecorator}");
                }
                sb.AppendLine($"    {name}{nullable}: {tsType};{comment}");
            }

            // Constructor 추가 (인터페이스가 아닌 경우에만)
            if (!isInterface)
            {
                sb.AppendLine();
                sb.AppendLine($"    constructor(data?: Partial<{className}>) {{");
                if (model.GetExtends().Any())
                {
                    sb.AppendLine("      super(data);");
                }
                sb.AppendLine("      if (data) {");

                foreach (var property in model.Properties)
                {
                    var propertyName = property.Name.ToCamel(removeLowdash: false);
                    var propertyType = property.Type;

                    if (propertyType == "DateTime")
                    {
                        sb.AppendLine($"        if (data.{propertyName}) this.{propertyName} = new Date(data.{propertyName});");
                    }
                    else if (propertyType.StartsWith("IEnumerable<") || propertyType.StartsWith("ICollection<"))
                    {
                        var itemType = propertyType.GetBetween("<", ">");
                        var m = FindModel(itemType);
                        if (m.Item1 != null)
                        {
                            sb.AppendLine($"        if (data.{propertyName}) this.{propertyName} = data.{propertyName}.map(item => new {m.Item1.ns}.{itemType}(item));");
                        }
                    }
                    else
                    {
                        var m = FindModel(propertyType);
                        if (m.Item1 != null)
                        {
                            sb.AppendLine($"        if (data.{propertyName}) this.{propertyName} = new {m.Item1.ns}.{propertyType}(data.{propertyName});");
                        }
                        else
                        {
                            sb.AppendLine($"        if (data.{propertyName}) this.{propertyName} = data.{propertyName};");
                        }
                    }
                }

                sb.AppendLine("      }");
                sb.AppendLine("    }");
            }

            // replace
            sb.Replace("IEnumerable<", "Array<");
            sb.Replace("ICollection<", "Array<");

            sb.AppendLine($"  }}");
            sb.AppendLine();
        }

        public static IEnumerable<string> GetTypeHierarchy(string input)
        {
            return input.Split("<").Select(p => p.Replace(">", string.Empty));
        }

        private string ResolveTypeNames(string typeLine, List<string> usingLines)
        {
            foreach(var tName in typeLine.Split(","))
            {
                if (tName.Contains('<')) // is generic
                {
                    var baseName = tName.Left("<").Trim();
                    var parameters = tName.GetBetweenBlock("<", ">").Split(",").Select(p => p.Trim());

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

                var typeName = tName.Trim();
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

            return string.Empty;
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
