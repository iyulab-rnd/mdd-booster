using System.Text;

namespace MDDBooster.Builders;

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
        var dbsetLines = tables.Select(p => $"\tpublic DbSet<{p.Name}> {p.Name.ToPlural()} {{ get; set; }}");
        var dbSet = string.Join(Constants.NewLine, dbsetLines);

        var onModelCreatingText = GetOnModelCreatingText(tables);

        var code = $@"// # {Constants.NO_NOT_EDIT_MESSAGE}
using Microsoft.Extensions.Logging;

namespace {ns}.Services;

public partial class DataContext(IHttpContextAccessor httpContextAccessor, DbContextOptions options) : ODataContext(httpContextAccessor, options)
{{
{dbSet}

#if DEBUG
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {{
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseLoggerFactory(LoggerFactory.Create(builder => {{ builder.AddConsole(); }}));
        optionsBuilder.EnableSensitiveDataLogging();
    }}
#endif

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        base.OnModelCreating(modelBuilder);
{onModelCreatingText}

        OnModelCreatingPartial(modelBuilder);
    }}

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}}
";

        var text = code.Replace("\t", "    ");
        var path = Path.Combine(basePath, $"DataContext.cs");
        Functions.FileWrite(path, text);
    }

    private static string GetOnModelCreatingText(IEnumerable<TableMeta> tables)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BuildOneWithManyByFKLines(tables).TrimEnd());
        sb.AppendLine(BuildEntityToTableLines(tables).TrimEnd());
        return sb.ToString();
    }

    private static string BuildOneWithManyByFKLines(IEnumerable<TableMeta> tables)
    {
        var sb = new StringBuilder();

        var map = new Dictionary<string, string>()
        {
            { "ON DELETE NO ACTION", "NoAction" },
            { "ON UPDATE NO ACTION", "NoAction" },
        };

        foreach (var table in tables)
        {
            foreach (var fkColumn in table.GetFkColumns())
            {
                var entityName = fkColumn.GetForeignKeyEntityName();
                var count = table.GetFkColumns().Where(p => p.GetForeignKeyEntityName() == entityName).Count();
                if (count < 2) continue; // 두개 이상인 것에 대해서만 추가 관계 설정

                var name = fkColumn.Name;
                var objName = Utils.GetNameWithoutKey(name);
                var manyName = $"{table.Name}{objName.ToPlural()}";
                if (objName == table.Name)
                {
                    objName += "Item";
                }

                var deleteOption = "NoAction"; // 기본값
                var updateOption = "NoAction"; // 기본값

                var option = fkColumn.GetForeignKeyOption();
                if (option != null)
                {
                    if (option.Contains("OnDelete") || option.Contains("OnUpdate"))
                    {
                        var parts = option.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(p => p.Trim());

                        foreach (var part in parts)
                        {
                            if (part.StartsWith("OnDelete"))
                            {
                                deleteOption = part.GetBetween("(", ")");
                            }
                            else if (part.StartsWith("OnUpdate"))
                            {
                                updateOption = part.GetBetween("(", ")");
                            }
                        }
                    }
                    else if (map.TryGetValue(option, out string? value))
                    {
                        deleteOption = value;
                    }
                }
                else
                {
                    deleteOption = fkColumn.NN == true ? "Cascade" : "SetNull";
                }

                var line = $@"
            modelBuilder.Entity<{table.Name}>()
                .HasOne(f => f.{objName})
                .WithMany(a => a.{manyName})
                .HasForeignKey(f => f.{name})
                .OnDelete(DeleteBehavior.{deleteOption});";
                sb.AppendLine(line);
            }
        }

        return sb.ToString();
    }

    private static string BuildEntityToTableLines(IEnumerable<TableMeta> tables)
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

                        var manyName = Utils.GetVirtualManyName(table);
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
