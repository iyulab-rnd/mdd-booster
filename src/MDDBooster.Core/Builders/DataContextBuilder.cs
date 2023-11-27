using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MDDBooster.Builders
{
    public class DataContextBuilder
    {
        private readonly IModelMeta[] models;

        public DataContextBuilder(IModelMeta[] models)
        {
            this.models = models;
        }


        public void Build(string modelNS, string ns, string basePath)
        {
            var tables = models.OfType<TableMeta>();
            var dbsetLines = tables.Select(p => $"\t\tpublic DbSet<{p.Name}> {p.Name.ToPlural()} {{ get; set; }}");
            var dbSet = string.Join(Constants.NewLine, dbsetLines);

            var onModelCreatingText = GetOnModelCreatingText(tables);

            var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using Iyu.Data;
using Microsoft.EntityFrameworkCore;
using {modelNS}.Entity;

namespace {ns}.Services
{{
    public partial class DataContext : DataContextBase
    {{
{dbSet}

#pragma warning disable CS8618
        public DataContext(DbContextOptions options) : base(options)
        {{
        }}
#pragma warning restore CS8618

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {{
            base.OnModelCreating(modelBuilder);
{onModelCreatingText}
        }}
    }}
}}";

            var text = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"DataContext.cs");
            File.WriteAllText(path, text);
        }

        private static string GetOnModelCreatingText(IEnumerable<TableMeta> tables)
        {
            var sb = new StringBuilder();
            foreach (var table in tables)
            {
                if (table.GetChildren().Any())
                {
                    var line = @$"
            modelBuilder.Entity<{table.Name}>().ToTable(tb => tb.HasTrigger(""{table.Name}Trigger""));";
                    sb.AppendLine(line);
                }

                foreach (var column in table.Columns)
                {
                    if (column.FK && column.Name.Contains('_') != true)
                    {
                        var pName = Utils.GetNameWithoutKey(column.Name);
                        var byName = $"{table.Name.ToPlural()}By{pName}";
                        var line = @$"
            modelBuilder.Entity<{table.Name}>()
              .HasOne(e => e.{pName})
              .WithMany(e => e.{byName})
              .OnDelete(DeleteBehavior.NoAction);";
                        sb.AppendLine(line);
                    }
                }   
            }
            return sb.ToString();
        }
    }
}
