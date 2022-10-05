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

        protected static string OutputPropertyLine(ColumnMeta c)
        {
            var attributesText = c.Attributes == null ? null : string.Join($"{Environment.NewLine}\t\t", c.Attributes);
            if (attributesText != null) 
            {
                attributesText += $"{Environment.NewLine}\t\t";
            }

            var typeAlias = c.GetSystemTypeAlias();
            var nullable = c.NN == null || (bool)c.NN == false ? "?" : string.Empty;
            return @$"{attributesText}public {typeAlias}{nullable} {c.Name} {{ get; set; }}";
            //[ForeignKey(TableName = ""MaterialItem"", ColumnName = ""_id"", Options = ForeignKeyOptions.Delete)]
            //[Binding]
            //[Display(Name = ""사용자재품목ID"", Order = 1, AutoGenerateField = false)]
            //[Required(ErrorMessageResourceName = ""Required"", ErrorMessageResourceType = typeof(Iyu.Properties.ValidationResources))]
            //[DataField]
            //public int UseMaterialItemId {{ get; set; } }
        }

    }

    internal class InterfaceBuilder : ModelBuilder
    {
        public InterfaceBuilder(ModelMetaBase m) : base(m)
        {
        }

        public void Build(string ns, string basePath)
        {
            var propertyLines = Columns.Select(p => OutputPropertyLine(p));
            var propertyLinesText = string.Join($"{Environment.NewLine}{Environment.NewLine}\t\t", propertyLines);

            var className = Name;
            var baseText = meta.Interfaces == null || meta.Interfaces.Length == 0
                ? string.Empty
                : string.Join(", ", meta.Interfaces.Select(p => p.Name));

            var baseLine = string.IsNullOrWhiteSpace(baseText)
                ? string.Empty
                : $" : {baseText}";

            var code = $@"using System.ComponentModel.DataAnnotations.Schema;

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
            var propertyLines = FullColumns.Select(p => OutputPropertyLine(p));
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

            var code = $@"using System.ComponentModel.DataAnnotations.Schema;

namespace {ns}.Data.Entity
{{
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
    }

}