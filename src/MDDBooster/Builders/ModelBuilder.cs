using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace MDDBooster.Builders
{
    internal abstract class ModelBuilder : BuilderBase
    {
        public ModelBuilder(ModelMetaBase m) : base(m)
        {
        }

        protected static string[] OutputPropertyLines(ColumnMeta c)
        {
#if DEBUG
            if (c.Name == "PlanType")
            {
            }
#endif
            var attributesText = c.Attributes == null ? null : string.Join($"{Environment.NewLine}\t\t", c.Attributes);
            if (string.IsNullOrEmpty(attributesText) != true) 
            {
                attributesText += $"{Environment.NewLine}\t\t";
            }

            var typeAlias = c.GetSystemTypeAlias();
            var nullable = c.NN == null || (bool)c.NN == false ? "?" : string.Empty;

            var lines = new List<string>()
            {
                @$"{attributesText}public {typeAlias}{nullable} {c.Name} {{ get; set; }}"
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

                var line = $@"[Ignore]
		[JsonIgnore]
		public {typeName} {name}
		{{
			get => {getter}
			set => {setter}
        }}";
                lines.Add(line);
            }
            return lines.ToArray();
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

            var code = $@"using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace {ns}.Data.Entity
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
            var propertyLines = FullColumns.SelectMany(p => OutputPropertyLines(p));
            var propertyLinesText = string.Join($"{Environment.NewLine}{Environment.NewLine}\t\t", propertyLines);

            var summary = Name;
            var className = Name;
            var tableName = Name;

            var baseText = meta.Interfaces == null || meta.Interfaces.Length == 0
    ? "IEntity"
    : string.Join(", ", meta.Interfaces.Select(p => p.Name));

            var baseLine = string.IsNullOrWhiteSpace(baseText)
                ? string.Empty
                : $" : {baseText}";

            var enumSyntax = GetEnumSyntax();

            var code = $@"using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace {ns}.Data.Entity
{{{enumSyntax}
    /// <summary>
    /// {summary}
    /// </summary>
    [Table(name: ""{tableName}"")]
    public partial class {className}{baseLine}
    {{
		{propertyLinesText}
    }}
}}";
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