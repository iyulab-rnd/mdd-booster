namespace MDDBooster.Builders
{
    public class EntitySetBuilder
    {
        private readonly IModelMeta[] models;

        public EntitySetBuilder(IModelMeta[] models)
        {
            this.models = models;
        }

        public void Build(string modelNS, string serverNS, string basePath)
        {
            var tables = models.OfType<TableMeta>();

            var addfuncLines = tables.Select(p => $"\t\t\tesBuilder.Add{p.Name}(builder);");
            var addfunc = string.Join(Constants.NewLine, addfuncLines);

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
            var methods = string.Join(Constants.NewLine, methodLines);

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}

namespace {serverNS}.Services
{{
    public partial class EntitySetBuilder
    {{
        partial void AddCustom(ODataConventionModelBuilder builder);

        public static ODataConventionModelBuilder AddAll(ODataConventionModelBuilder builder)
        {{
            var esBuilder = new EntitySetBuilder();
{addfunc}
            esBuilder.AddCustom(builder);

            return builder;
        }}
{methods}
    }}
}}

#pragma warning restore CS8618, IDE1006";

            var text = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"EntitySetBuilder.cs");
            Functions.FileWrite(path, text);
        }
    }
}
