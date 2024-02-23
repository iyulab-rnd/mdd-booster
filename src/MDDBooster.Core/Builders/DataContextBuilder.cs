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

namespace {ns}.Services
{{
    public partial class DataContext(IHttpContextAccessor httpContextAccessor, DbContextOptions options) : ODataContext(httpContextAccessor, options)
    {{
{dbSet}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {{
            base.OnModelCreating(modelBuilder);
{onModelCreatingText}
        }}
    }}
}}";

            var text = code.Replace("\t", "    ");
            var path = Path.Combine(basePath, $"DataContext.cs");
            Functions.FileWrite(path, text);
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
                        var sysType = column.GetSystemType();
                        if (sysType == typeof(string))
                        {
                            var fkEntityName = column.GetForeignKeyEntityName();
                            var fkColumnName = column.GetForeignKeyColumnName();
                            var oneName = Utils.GetVirtualOneName(table, column);

                            var line = @$"
            modelBuilder.Entity<{fkEntityName}>().HasAlternateKey(p => p.{fkColumnName});";
                            sb.AppendLine(line);

                            var manyName = Utils.GetVirtualManeName(table);
                            line = $@"
            modelBuilder.Entity<{table.Name}>()
                .HasOne(p => p.{oneName})
                .WithMany(p => p.{manyName})
                .HasForeignKey(p => p.{column.Name})
                .HasPrincipalKey(p => p.{fkColumnName});";
                            sb.AppendLine(line);
                        }

                        //var pName = Utils.GetNameWithoutKey(column.Name);
                        //var byName = Utils.GetVirtualManeName(table, null, null);
                        
                        /*
                                    // CommonCodeGroup에서 GroupCode를 대체 키로 설정


                                    // CommonCode의 GroupCode를 외래 키로 설정

                         */
                    }
                }
            }
            return sb.ToString();
        }
    }
}
