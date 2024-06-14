﻿using System;
using System.Text;

namespace MDDBooster.Builders
{
    public abstract class ModelBuilder : BuilderBase
    {
        public ModelBuilder(ModelMetaBase m) : base(m)
        {
        }

        protected static Settings.Settings Settings => Resolver.Settings ?? throw new InvalidOperationException("Settings is null.");

        protected string[] OutputPropertyLines(ColumnMeta c)
        {
#if DEBUG
            if (c.Name == "ConsumerPrice")
            {
            }
#endif
            var attributeLines = BuildAttributesLines(c);
            var attributesText = string.Join($"{Constants.NewLine}\t\t", attributeLines);
            if (string.IsNullOrEmpty(attributesText) != true) 
            {
                attributesText += $"{Constants.NewLine}\t\t";
            }

            var sysType = c.GetSystemType();
            var typeAlias = c.GetSystemTypeAlias();

            var nullable = c.NN == null || (bool)c.NN == false ? "?" : string.Empty;
            var required = c.NN != null && c.NN == true ? "required " : string.Empty;

            var defaultText = string.Empty;
            if (string.IsNullOrWhiteSpace(c.Default) != true)
            {
                if (c.Default.Contains("@by"))
                    defaultText = $" = {c.Default};";

                else if (c.Default.Contains("@now"))
                    defaultText = $" = DateTime.Now;";

                else if (sysType == typeof(bool))
                {
                    if (c.Default == "1")
                        defaultText = $" = true;";

                    else if (c.Default == "0")
                        defaultText = $" = false;";

                    else if (bool.TryParse(c.Default, out var bDefault))
                        defaultText = $" = {bDefault.ToString().ToLower()};";

                    else
                        defaultText = $" = {c.Default.ToLower()};";
                }
                else if (sysType == typeof(string))
                {
                    if (c.Default.StartsWith('\"'))
                        defaultText = $" = {c.Default};";

                    else
                        defaultText = $" = \"{c.Default}\";";
                }
                else
                {
                    defaultText = $" = {c.Default};";
                }

                required = string.Empty;
            }

            var isInterface = this.meta is InterfaceMeta;
            var publicText = isInterface ? string.Empty : "public ";
            var requiredText = isInterface ? string.Empty : required;
            if (isInterface) defaultText = string.Empty;

            var lines = new List<string>()
            {
                @$"{attributesText}{publicText}{requiredText}{typeAlias}{nullable} {c.Name} {{ get; set; }}{defaultText}"
            };

            if (c.IsEnumType())
            {
                var typeName = c.GetEnumTypeName();
                var name = c.Name + "Enum";
                var vName = name.ToCamel();
                string getter, setter;
                if (c.IsEnumKey())
                {
                    getter = $"Enum.TryParse(typeof({typeName}), {c.Name}, out var {vName}) ? ({typeName}){vName}! : default;";
                    setter = $"{c.Name} = value.ToString();";
                }
                else
                {
                    getter = $"Enum.IsDefined(typeof({typeName}), {c.Name}) ? ({typeName}){c.Name} : default;";
                    setter = $"{c.Name} = (int)value;";
                }

                var line = $@"[NotMapped]
        [Ignore]
		public virtual {typeName} {name}
		{{
			get => {getter}
			set => {setter}
        }}";
                lines.Add(line);
            }

            return [.. lines];
        }

        private static readonly string[] ignoreAttributeName = ["PK", "Unique", "UQ", "UI", "FK", "Index", "desc"];
        private static IEnumerable<string> BuildAttributesLines(ColumnMeta c)
        {
            if (c.Name == "Cost")
            {

            }
            if (c.PK) yield return "[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]";

            if (c.IsNotNull()) yield return "[Required]";

            // Display
            var displaySb = new StringBuilder();
            displaySb.Append($"[Display(Name = \"{c.Label}\"");
            if (c.ShortName != null) displaySb.Append($", ShortName = \"{c.ShortName}\"");
            if (c.Description != null) displaySb.Append($", Description = \"{c.Description}\"");
            yield return $"{displaySb})]";

            if (c.FK)
            {
                string typeName;
                string? propName = null;
                var attr = c.Attributes.FirstOrDefault(p => string.Equals(p.Name, "FK", StringComparison.OrdinalIgnoreCase));
                if (attr != null && attr.Value != null)
                {
                    typeName = attr.Value.LeftOr(".");
                    propName = attr.Value.Right(".").LeftOr(",").Trim();
                }
                else
                {
                    typeName = c.Name.LeftOr("_");
                }

                if (propName == "_id" || propName == "_key") propName = null;

                var fkText = c.LineText.GetBetween("[FK", "]", false, include: true);
                var fkConstants = fkText.GetBetween(",", "]").Trim();
                var fkConstantsText = string.IsNullOrWhiteSpace(fkConstants) ? string.Empty : $", Constants = \"{fkConstants}\"";

                if (string.IsNullOrEmpty(propName))
                    yield return $"[FK(typeof({typeName}){fkConstantsText})]";
                else
                    yield return $"[FK(typeof({typeName}), PropertyName = nameof(Entity.{typeName}.{propName}){fkConstantsText})]";
            }

            // MaxLength
            if (c.GetSize() is string size)
            {   
                if (c.GetMaxLength() is int maxLength)
                {
                    yield return $"[MaxLength({maxLength})]";

                    if (maxLength >= 300)
                    {
                        yield return $"[Multiline]";
                    }
                }
                else if (size == "max")
                {
                    yield return $"[Multiline]";
                }
            }

            if (c.UQ)
            {
                yield return $"[Unique]";
            }

            var sqlType = c.GetSqlType();
            if (sqlType == "MONEY")
            {
                yield return $"[Column(TypeName = \"money\")]";
            }

            foreach(var attribute in c.Attributes)
            {
                if (ignoreAttributeName.Contains(attribute.Name)) continue;

                yield return $"[{attribute.Line.Trim()}]";
            }
        }

        public IEnumerable<string> BuildFKLines()
        {
            var lines = new List<string>();
            foreach(var column in this.Columns.Where(p => p.FK))
            {
                var c = column;
                var typeName = c.GetForeignKeyEntityName();
                var oneName = Utils.GetNameWithoutKey(column.Name);
                if (this.Columns.Any(p => p.Name == oneName) || oneName == this.Name)
                {
                    oneName += "Item";
                }

                string line;
                if (column.IsNotNull())
                {
                    line = $@"[ForeignKey(nameof({c.Name}))]
		public virtual {typeName} {oneName} {{ get; set; }} = null!;";
                }
                else
                {
                    line = $@"[ForeignKey(nameof({c.Name}))]
		public virtual {typeName}? {oneName} {{ get; set; }}";
                }
                lines.Add(line);
            }
            return lines;
        }

        public IEnumerable<string> BuildChildrenLines()
        {
            var lines = new List<string>();
            if (this.meta is TableMeta table)
            {
                var children = table.GetChildren();
                foreach(var child in children)
                {
                    var pNames = new List<string>();
                    foreach(var c in child.GetFkColumns())
                    {
                        var nm = c.GetForeignKeyEntityName();
                        if (table.Name != nm) continue;

                        var pName = Utils.GetVirtualManeName(child);
                        if (pNames.Contains(pName)) continue;
                        pNames.Add(pName);
                    }

                    if (pNames.Count == 0) continue;

                    foreach(var pName in pNames)
                    {
                        var line = $@"public virtual ICollection<{child.Name}>? {pName} {{ get; set; }}";
                        lines.Add(line);
                    }
                }
            }
            return lines;
        }
    }

    public class InterfaceBuilder : ModelBuilder
    {
        public InterfaceBuilder(ModelMetaBase m) : base(m)
        {
        }

        public void Build(string ns, string basePath)
        {
            var propertyLines = Columns.SelectMany(OutputPropertyLines);
            var propertyLinesText = string.Join($"{Constants.NewLine}{Constants.NewLine}\t\t", propertyLines);

            var className = Name;
            var baseText = meta.Interfaces == null || meta.Interfaces.Length == 0
                ? string.Empty
                : string.Join(", ", meta.Interfaces.Select(p => p.Name));

            var baseLine = string.IsNullOrWhiteSpace(baseText)
                ? string.Empty
                : $" : {baseText}";

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}

namespace {ns}.Entity
{{
    public partial interface {className}{baseLine}
    {{
        {propertyLinesText}
    }}
}}";
            code = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"{className}.cs");
            Functions.FileWrite(path, code);
        }
    }

    public class EntityBuilder : ModelBuilder
    {
        public EntityBuilder(ModelMetaBase m) : base(m)
        {
        }

        public void Build(string ns, string basePath)
        {
            var columns = meta.FullColumns;

            if (meta.Abstract != null)
            {
                columns = columns.Where(p =>
                {
                    return meta.Abstract.FullColumns.Any(n => n.Name == p.Name) != true;
                }).ToArray();
            }

            var propertyLines = columns
                .SelectMany(p => OutputPropertyLines(p))
                .Concat(this.BuildFKLines())
                .Concat(this.BuildChildrenLines());
            var propertyLinesText = string.Join($"{Constants.NewLine}{Constants.NewLine}\t\t", propertyLines);

            var summary = Name;
            var className = Name;
            var tableName = Name;

            var baseEntities = meta.Abstract == null
                ? []
                : new string[] { meta.Abstract.Name };

            if (meta.Interfaces != null)
            {
                baseEntities = [.. baseEntities, .. meta.Interfaces.Select(p => p.Name)];
            }

            if (meta.Extensions != null)
            {
                foreach(var extension in meta.Extensions)
                {
                    if (baseEntities.Contains(extension)) continue;

                    baseEntities = [.. baseEntities, extension];
                }
            }

            var inheritsText = string.IsNullOrEmpty(this.meta.Inherits)
                ? string.Join(", ", baseEntities)
                : this.meta.Inherits;

            var baseLine = string.IsNullOrWhiteSpace(inheritsText)
                ? $" : IEntity"
                : $" : {inheritsText}";

            var enumSyntax = GetEnumSyntax();

            string code;
            if (meta is AbstractMeta abstractMeta)
            {
                code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}

namespace {ns}.Entity
{{{enumSyntax}
    public abstract partial class {className}{baseLine}
    {{
		{propertyLinesText}
    }}
}}";
            }
            else if (meta is TableMeta tableMeta)
            {
                var attributes = new List<string>
                {
                    $"[Table(name: \"{tableName}\")]"
                };
                if (string.IsNullOrEmpty(tableMeta.Label) != true)
                {
                    attributes.Add($"[DisplayName(\"{tableMeta.Label}\")]");
                }
                var attributesText = string.Join("\r\n\t", attributes);
                code = $@"// {Constants.NO_NOT_EDIT_MESSAGE}

namespace {ns}.Entity
{{{enumSyntax}
    /// <summary>
    /// {summary}
    /// </summary>
    {attributesText}
    public partial class {className}{baseLine}
    {{
		{propertyLinesText}
    }}
}}";
            }
            else
                throw new NotImplementedException();

            var path = Path.Combine(basePath, $"{className}.cs");
            Functions.FileWrite(path, code);
        }

        private static readonly List<string> enumDefinitions = [];

        private string? GetEnumSyntax()
        {
            var list = new List<string>();
            foreach(var c in FullColumns.Where(p => p.IsEnumType()))
            {
                var options = c.GetEnumOptions();
                if (options != null)
                {
                    var name = c.GetEnumTypeName();
                    if (enumDefinitions.Contains(name)) continue;
                    enumDefinitions.Add(name);

                    var optionsLinesText = string.Join($",{Constants.NewLine}\t\t", options);
                    var line = $@"public enum {name}
	{{
		{optionsLinesText}
	}}";
                    list.Add(line);
                }
            }

            if (list.Count != 0)
                return $"{Constants.NewLine}\t{string.Join(Constants.NewLine + Constants.NewLine, list)}{Constants.NewLine}";

            else
                return null;
        }
    }

}