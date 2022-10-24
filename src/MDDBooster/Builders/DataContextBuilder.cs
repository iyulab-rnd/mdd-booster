using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MDDBooster.Builders
{
    internal class DataContextBuilder
    {
        private IModelMeta[] models;

        public DataContextBuilder(IModelMeta[] models)
        {
            this.models = models;
        }

        internal void Build(string modelNS, string ns, string basePath)
        {
            var tables = models.OfType<TableMeta>();
            var dbsetLines = tables.Select(p => $"\t\tpublic DbSet<{p.Name}> {p.Name.ToPlural()} {{ get; set; }}");
            var dbSet = string.Join(Environment.NewLine, dbsetLines);

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using Iyu.Data;
using Microsoft.EntityFrameworkCore;
using {modelNS}.Entity;

namespace {ns}.Services
{{
    public partial class DataContext : DataContextBase
    {{
{dbSet}

        public DataContext(DbContextOptions options) : base(options)
        {{
        }}
    }}
}}";

            var text = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"DataContext.cs");
            File.WriteAllText(path, text);
        }
    }
}
