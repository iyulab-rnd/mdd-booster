namespace MDDBooster.Builders
{
    public class DataControllerBuilder
    {
        private readonly IModelMeta[] models;

        public DataControllerBuilder(IModelMeta[] models)
        {
            this.models = models;
        }

        public void Build(string modelNS, string serverNS, string basePath)
        {
            var tables = models.OfType<TableMeta>();

            var methodLines = new List<string>();
            foreach(var table in tables)
            {
                var name = table.Name.ToPlural();
                var line = $@"
        [HttpGet(""{name}"")]
        [EnableQuery]
        public IActionResult Get{name}()
        {{
            return Ok(data.{name});
        }}";
                methodLines.Add(line);
            }

            var methodLinesText = string.Join(Environment.NewLine, methodLines);

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}

using Iyu.Server.OData.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;

namespace {serverNS}.Controllers
{{
    public partial class ODataController(IDataContext data) : ODataControllerBase
    {{
        protected readonly DataContext data = (DataContext)data;
{methodLinesText}
    }}
}}";

            var text = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"ODataController.cs");
            File.WriteAllText(path, text);
        }
    }
}
