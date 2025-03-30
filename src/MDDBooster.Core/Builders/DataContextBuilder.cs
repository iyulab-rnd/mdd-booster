using MDDBooster.Models;
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

                // 자기 참조 관계 처리
                if (entityName == table.Name)
                {
                    var refObjName = Utils.GetNameWithoutKey(fkColumn.Name);

                    // EntityRelationshipHelper를 사용하여 컬렉션 속성 이름 결정
                    string collectionName = EntityRelationshipHelper.DetermineCollectionPropertyName(table.Name, fkColumn.Name);

                    var refDeleteOption = DetermineDeleteOption(fkColumn);

                    var selfRefLine = $@"
            modelBuilder.Entity<{table.Name}>()
                .HasOne(f => f.{refObjName})
                .WithMany(a => a.{collectionName})
                .HasForeignKey(f => f.{fkColumn.Name})
                .OnDelete(DeleteBehavior.{refDeleteOption});";

                    sb.AppendLine(selfRefLine);
                    continue;
                }

                // 일반적인 관계 처리 (기존 코드)
                var count = table.GetFkColumns().Where(p => p.GetForeignKeyEntityName() == entityName).Count();
                if (count < 2) continue; // 두개 이상인 것에 대해서만 추가 관계 설정

                var name = fkColumn.Name;
                // Use EntityRelationshipHelper to determine proper navigation property names
                var propObjName = EntityRelationshipHelper.DetermineNavigationPropertyName(entityName, name, table.Name);
                var manyName = $"{table.Name}{Utils.GetNameWithoutKey(name).ToPlural()}";

                var propDeleteOption = "NoAction"; // 기본값
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
                                propDeleteOption = part.GetBetween("(", ")");
                            }
                            else if (part.StartsWith("OnUpdate"))
                            {
                                updateOption = part.GetBetween("(", ")");
                            }
                        }
                    }
                    else if (map.TryGetValue(option, out string? value))
                    {
                        propDeleteOption = value;
                    }
                }
                else
                {
                    propDeleteOption = fkColumn.NN == true ? "Cascade" : "SetNull";
                }

                var regularLine = $@"
            modelBuilder.Entity<{table.Name}>()
                .HasOne(f => f.{propObjName})
                .WithMany(a => a.{manyName})
                .HasForeignKey(f => f.{name})
                .OnDelete(DeleteBehavior.{propDeleteOption});";
                sb.AppendLine(regularLine);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 외래 키의 삭제 동작 옵션을 결정합니다.
    /// </summary>
    /// <param name="fkColumn">외래 키 컬럼</param>
    /// <returns>DeleteBehavior 유형 이름</returns>
    private static string DetermineDeleteOption(ColumnMeta fkColumn)
    {
        var option = fkColumn.GetForeignKeyOption();

        // 1. 명시적으로 지정된 옵션 처리
        if (option != null)
        {
            // OnDelete 구문 처리 (OnDelete(Cascade)와 같은 패턴)
            if (option.Contains("OnDelete"))
            {
                var parts = option.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim());

                foreach (var part in parts)
                {
                    if (part.StartsWith("OnDelete"))
                    {
                        return part.GetBetween("(", ")");
                    }
                }
            }

            // SQL 스타일 옵션 처리 (ON DELETE CASCADE와 같은 패턴)
            if (option.Contains("NO ACTION", StringComparison.OrdinalIgnoreCase))
                return "NoAction";
            if (option.Contains("CASCADE", StringComparison.OrdinalIgnoreCase))
                return "Cascade";
            if (option.Contains("SET NULL", StringComparison.OrdinalIgnoreCase))
                return "SetNull";
        }

        // 2. 특별한 관계 패턴 처리 (자기 참조 등)
        if (fkColumn.Name.EndsWith("Parent_id") || fkColumn.Name.EndsWith("ThreadRoot_id"))
        {
            return "NoAction";
        }

        // 3. 기본 동작: NULL 허용 여부에 따라 동작 결정
        return fkColumn.NN == true ? "Cascade" : "SetNull";
    }

    private static string BuildEntityToTableLines(IEnumerable<TableMeta> tables)
    {
        var sb = new StringBuilder();
        foreach (var table in tables)
        {
            if (table.GetChildren().Any())
            {
                var entityTableLine = @$"
            modelBuilder.Entity<{table.Name}>().ToTable(tb => tb.HasTrigger(""{table.Name}Trigger""));";
                sb.AppendLine(entityTableLine);
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

                        var alternateKeyLine = @$"
            modelBuilder.Entity<{fkEntityName}>().HasAlternateKey(p => p.{fkColumnName});";
                        sb.AppendLine(alternateKeyLine);

                        var manyName = Utils.GetVirtualManyName(table);
                        var relationLine = $@"
            modelBuilder.Entity<{table.Name}>()
                .HasOne(p => p.{oneName})
                .WithMany(p => p.{manyName})
                .HasForeignKey(p => p.{column.Name})
                .HasPrincipalKey(p => p.{fkColumnName});";
                        sb.AppendLine(relationLine);
                    }
                }
            }
        }
        return sb.ToString();
    }
}