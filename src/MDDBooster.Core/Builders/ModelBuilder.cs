﻿using System.Text;

namespace MDDBooster.Builders
{
    public abstract class ModelBuilder : BuilderBase
    {
        public ModelBuilder(ModelMetaBase m) : base(m)
        {
        }

        protected static Settings.Settings Settings => Resolver.Settings;

        protected string[] OutputPropertyLines(ColumnMeta c)
        {
#if DEBUG
            if (c.Name == "CreatedBy")
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
                        defaultText = $" = {bDefault};";

                    else
                        defaultText = $" = {c.Default};";
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

            return lines.ToArray();
        }

        private static readonly string[] ignoreAttributeName = new string[] { "PK", "Unique", "UQ", "UI", "FK", "Index", "desc" };
        private static IEnumerable<string> BuildAttributesLines(ColumnMeta c)
        {
            if (c.Name == "MemberKey")
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
                var attr = c.Attributes.FirstOrDefault(p => string.Equals(p.Name, "FK", StringComparison.OrdinalIgnoreCase));
                if (attr != null && attr.Value != null)
                {
                    typeName = attr.Value.LeftOr(".");
                }
                else
                {
                    typeName = c.Name.LeftOr("_");
                }

                yield return $"[FK(typeof({typeName}))]";
            }

            // MaxLength
            if (c.GetSize() is string size && size != "max")
            {
                yield return $"[MaxLength({size})]";
            }

            foreach(var attribute in c.Attributes)
            {
                if (ignoreAttributeName.Contains(attribute.Name)) continue;

                yield return $"[{attribute.Line}]";
            }
        }

        public IEnumerable<string> BuildFKLines()
        {
            var lines = new List<string>();
            foreach(var column in this.Columns.Where(p => p.FK))
            {
                var c = column;
                var pName = Utils.GetNameWithoutKey(c.Name);
                var typeName = c.GetForeignKeyEntityName();

                if (this.Columns.Any(p => p.Name == pName))
                {
                    pName += "Item"; 
                }

                var line = $@"[ForeignKey(nameof({c.Name}))]
		public virtual {typeName}? {pName} {{ get; set; }}";
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
                    foreach(var c in child.GetFkColumns())
                    {
                        var nm = c.GetForeignKeyEntityName();
                        if (table.Name != nm) continue;

                        var pName = child.Name.ToPlural();
                        pName = pName + "By" + Utils.GetNameWithoutKey(c.Name);
                        var line = $@"public virtual List<{child.Name}>? {pName} {{ get; set; }}";
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
            File.WriteAllText(path, code);
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
                ? Array.Empty<string>()
                : new string[] { meta.Abstract.Name };

            if (meta.Interfaces != null)
            {
                baseEntities = baseEntities.Concat(meta.Interfaces.Select(p => p.Name)).ToArray();
            }

            if (meta.Extensions != null)
            {
                foreach(var extension in meta.Extensions)
                {
                    if (baseEntities.Contains(extension)) continue;

                    baseEntities = baseEntities.Append(extension).ToArray();
                }
            }

            var baseText = string.Join(", ", baseEntities);

            var baseLine = string.IsNullOrWhiteSpace(baseText)
                ? $" : IEntity"
                : $" : {baseText}";

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
                if (tableMeta.Label != tableMeta.Name)
                {
                    attributes.Add($"[DisplayName(\"{tableName}\")]");
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
            File.WriteAllText(path, code);
        }

        private static readonly List<string> enumDefinitions = new();

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

            if (list.Any())
                return $"{Constants.NewLine}\t{string.Join(Constants.NewLine + Constants.NewLine, list)}{Constants.NewLine}";

            else
                return null;
        }
    }

}