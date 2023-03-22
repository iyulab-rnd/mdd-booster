using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace MDDBooster.Builders
{
    internal abstract class ModelBuilder : BuilderBase
    {
        public ModelBuilder(ModelMetaBase m) : base(m)
        {
        }

        protected Settings.Settings Settings => Resolver.Settings;

        protected static string[] OutputPropertyLines(ColumnMeta c)
        {
#if DEBUG
            if (c.Name == "OwnerKey")
            {
            }
#endif
            var attributesText = c.Attributes == null ? null : string.Join($"{Environment.NewLine}\t\t", c.Attributes);
            if (string.IsNullOrEmpty(attributesText) != true) 
            {
                attributesText += $"{Environment.NewLine}\t\t";
            }

            var sysType = c.GetSystemType();
            var typeAlias = c.GetSystemTypeAlias();

            var nullable = c.NN == null || (bool)c.NN == false ? "?" : string.Empty;
            var required = c.NN != null && c.NN == true ? "required " : string.Empty;

            var defaultText = string.Empty;
            if (c.Default != null)
            {
                if (c.Default.Contains("@by"))
                    defaultText = $" = {c.Default};";

                else if (c.Default.Contains("@now"))
                    defaultText = $" = DateTime.Now;";
                
                else
                    defaultText = $" = {c.Default};";

                required = string.Empty;
            }

            var lines = new List<string>()
            {
                @$"{attributesText}public {required}{typeAlias}{nullable} {c.Name} {{ get; set; }}{defaultText}"
            };

            if (c.IsEnumType())
            {
                var typeName = StringHelper.ToPlural(c.Name);
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

        public IEnumerable<string> BuildFKLines()
        {
            var lines = new List<string>();
            foreach(var column in this.Columns.Where(p => p.FK))
            {
                var c = column;
                var pName = Utils.GetNameWithoutKey(c.Name);
                var typeName = c.GetForeignKeyEntityName();

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
                        if (c.Name.EndsWith("Key"))
                            pName = pName + "By" + c.Name.LeftOr("Key");

                        var line = $@"public virtual List<{child.Name}>? {pName} {{ get; set; }}";
                        lines.Add(line);
                    }
                }
            }
            return lines;
        }

        protected string BuildUsings()
        {
            var defaultUsing = @"using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;";

            if (Settings.ModelProject.Usings == null) return defaultUsing;

            var usings = Settings.ModelProject.Usings.Select(p => $"using {p};");
            var text = Environment.NewLine + string.Join(Environment.NewLine, usings);
            return defaultUsing + text;
        }
    }

    internal class InterfaceBuilder : ModelBuilder
    {
        public InterfaceBuilder(ModelMetaBase m) : base(m)
        {
        }

        public void Build(string ns, string basePath)
        {
            var propertyLines = Columns.SelectMany(p => OutputPropertyLines(p));
            var propertyLinesText = string.Join($"{Environment.NewLine}{Environment.NewLine}\t\t", propertyLines);

            var className = Name;
            var baseText = meta.Interfaces == null || meta.Interfaces.Length == 0
                ? string.Empty
                : string.Join(", ", meta.Interfaces.Select(p => p.Name));

            var baseLine = string.IsNullOrWhiteSpace(baseText)
                ? string.Empty
                : $" : {baseText}";

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
{BuildUsings()}

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

    internal class EntityBuilder : ModelBuilder
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
            var propertyLinesText = string.Join($"{Environment.NewLine}{Environment.NewLine}\t\t", propertyLines);

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
{BuildUsings()}

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
{BuildUsings()}

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

        private string? GetEnumSyntax()
        {
            var list = new List<string>();
            foreach(var c in FullColumns.Where(p => p.IsEnumType()))
            {
                var options = c.GetEnumOptions();
                if (options != null)
                {
                    var name = c.Name.ToPlural();
                    var optionsLinesText = string.Join($",{Environment.NewLine}\t\t", options);
                    var line = $@"public enum {name}
	{{
		{optionsLinesText}
	}}";
                    list.Add(line);
                }
            }

            if (list.Any())
                return $"{Environment.NewLine}\t{string.Join(Environment.NewLine + Environment.NewLine, list)}{Environment.NewLine}";

            else
                return null;
        }
    }

}