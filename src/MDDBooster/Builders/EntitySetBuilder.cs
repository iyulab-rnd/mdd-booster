namespace MDDBooster.Builders
{
    internal class EntitySetBuilder
    {
        private IModelMeta[] models;

        public EntitySetBuilder(IModelMeta[] models)
        {
            this.models = models;
        }

        internal void Build(string modelNS, string serverNS, string basePath)
        {
            var tables = models.OfType<TableMeta>();

            var addfuncLines = tables.Select(p => $"\t\t\tesBuilder.Add{p.Name}(builder);");
            var addfunc = string.Join(Environment.NewLine, addfuncLines);

            var methodLines = tables.Select(p =>
            {
                var r = @$"
        public virtual ODataModelBuilder Add{p.Name}(ODataModelBuilder builder)
        {{
            builder.EntitySet<{p.Name}>(""{p.Name.ToPlural()}"");
            return builder;
        }}";
                return r;
            });
            var methods = string.Join(Environment.NewLine, methodLines);

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
#pragma warning disable CS8618, IDE1006

using Microsoft.OData.ModelBuilder;
using {modelNS}.Entity;

namespace {serverNS}.Services
{{
    public partial class EntitySetBuilder
    {{
        public static ODataConventionModelBuilder AddAll(ODataConventionModelBuilder builder)
        {{
            var esBuilder = new EntitySetBuilder();
{addfunc}

            return builder;
        }}
{methods}
    }}
}}

#pragma warning restore CS8618, IDE1006";

            var text = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"EntitySetBuilder.cs");
            File.WriteAllText(path, text);
        }
    }
}
