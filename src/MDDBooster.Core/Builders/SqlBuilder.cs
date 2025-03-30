using MDDBooster.Models;

namespace MDDBooster.Builders;

public class SqlBuilder : BuilderBase
{
    public SqlBuilder(TableMeta m) : base(m)
    {
    }

    public void Build(string basePath)
    {
#if DEBUG
        if (this.meta.Name == "Plan")
        {

        }
#endif
        var columnLines = FullColumns.Select(p => OutputColumnLine(p));
        var columnLinesText = string.Join("\r\n\t", columnLines);

        // 인덱스 생성
        var indexLines = new List<string>();

        // UI로 통합된 인덱스 속성을 가진 컬럼에 대한 인덱스
        foreach (var column in FullColumns.Where(p => p.UI))
        {
            indexLines.Add(OutputIndexLine(column));
        }

        // @index 지시문을 사용한 인덱스
        if (meta is TableMeta tableMeta)
        {
            foreach (var index in tableMeta.GetIndexes())
            {
                indexLines.Add(OutputMultiColumnIndexLine(index));
            }
        }

        var indexLinesText = string.Join(Constants.NewLine, indexLines);

        var fkLines = FullColumns.Where(p => p.FK).Select(p => OutputFKLine(p));
        var fkLinesText = string.Join(Constants.NewLine, fkLines);

        var uniqueLines = GetUniqueLines(out var nullableUniqueLines);
        var uniqueLinesText = string.Empty;
        if (uniqueLines != null && uniqueLines.Length != 0)
        {
            var line = string.Join($",{Constants.NewLine}\t", uniqueLines);
            uniqueLinesText = $"{Constants.NewLine}\t{line}";
        }
        var nullableUniqueLinesText = string.Empty;
        if (nullableUniqueLines != null && nullableUniqueLines.Length != 0)
        {
            var line = string.Join($"{Constants.NewLine}", nullableUniqueLines.Select(p => $"{p}{Constants.NewLine}GO"));
            nullableUniqueLinesText = $"{Constants.NewLine}{line}";
        }

        var code = $@"-- # {Constants.NO_NOT_EDIT_MESSAGE}
CREATE TABLE [dbo].[{Name}]
(
    {columnLinesText}{uniqueLinesText}
)
GO{nullableUniqueLinesText}";
        var text = string.Join(Constants.NewLine, code, indexLinesText, fkLinesText);

        text = text.Replace("\t", "    ");
        var path = Path.Combine(basePath, $"{Name}.sql");
        Functions.FileWrite(path, text);
    }

    private string[] GetUniqueLines(out string[] nullableUniqueLines)
    {
        var list = new List<string>();
        var nullableUniqueList = new List<string>();
        foreach (var c in this.meta.FullColumns)
        {
            if (c.UQ)
            {
                if (c.NN is bool notnull && notnull)
                {
                    var line = $"CONSTRAINT [UK_{Name}_{c.Name}] UNIQUE NONCLUSTERED ([{c.Name}])";
                    list.Add(line);
                }
                else
                {
                    var line = $"CREATE UNIQUE NONCLUSTERED INDEX [IDX_{Name}_{c.Name}] ON [{Name}]([{c.Name}]) WHERE [{c.Name}] IS NOT NULL";
                    nullableUniqueList.Add(line);
                }
            }
        }

        nullableUniqueLines = [.. nullableUniqueList];

        var uniqueMultiples = this.meta.GetUniqueMultiples();
        if (uniqueMultiples.Any() != true) return [.. list];

        foreach (var uniqueMultiple in uniqueMultiples)
        {
            var nm = string.Join("_", uniqueMultiple);
            var fields = uniqueMultiple.Select(p => $"[{p}] ASC");
            var fieldsText = string.Join(", ", fields);
            var line = $"CONSTRAINT [UK_{Name}_{nm}] UNIQUE NONCLUSTERED ({fieldsText})";
            list.Add(line);
        }

        return list.ToArray();
    }

    private string OutputIndexLine(ColumnMeta c)
    {
        // 고유한 인덱스 이름 생성 (단일 컬럼)
        var indexName = $"IX_{Name}_{c.Name}";

        return $@"CREATE NONCLUSTERED INDEX [{indexName}] ON [dbo].[{Name}]([{c.Name}] ASC)
GO";
    }

    private string OutputMultiColumnIndexLine(IndexMeta index)
    {
        var columns = index.Columns.Select(c => $"[{c}] ASC");
        var columnsText = string.Join(", ", columns);

        // 사용자 지정 인덱스 이름이 있으면 사용, 없으면 테이블명과 컬럼명 조합으로 생성
        var indexName = index.Name;
        if (string.IsNullOrEmpty(indexName))
        {
            // 컬럼명을 _ 로 연결하여 인덱스 이름 생성
            indexName = $"IX_{Name}_{string.Join("_", index.Columns)}";
        }

        return $@"CREATE NONCLUSTERED INDEX [{indexName}] ON [dbo].[{Name}]({columnsText})
GO";
    }

    private object OutputFKLine(ColumnMeta c)
    {
        string fkTable, cName;
        var m = Functions.FindTable(c.GetForeignKeyEntityName());
        fkTable = m.Name;
        cName = m.GetPKColumn().Name;

        string onDeleteSyntax = "";
        string onUpdateSyntax = "";
        var option = c.GetForeignKeyOption();

        if (option != null)
        {
            // OnDelete, OnUpdate 형식 직접 처리
            if (option.Contains("OnDelete") || option.Contains("OnUpdate"))
            {
                var parts = option.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim());

                foreach (var part in parts)
                {
                    if (part.StartsWith("OnDelete"))
                    {
                        var action = part.GetBetween("(", ")");
                        // Convert Pascal/camelCase to SQL syntax
                        if (action.Equals("NoAction", StringComparison.OrdinalIgnoreCase))
                            action = "NO ACTION";
                        else if (action.Equals("SetNull", StringComparison.OrdinalIgnoreCase))
                            action = "SET NULL";
                        else if (action.Equals("Cascade", StringComparison.OrdinalIgnoreCase))
                            action = "CASCADE";

                        onDeleteSyntax = $" ON DELETE {action}";
                    }
                    else if (part.StartsWith("OnUpdate"))
                    {
                        var action = part.GetBetween("(", ")");
                        // Convert Pascal/camelCase to SQL syntax
                        if (action.Equals("NoAction", StringComparison.OrdinalIgnoreCase))
                            action = "NO ACTION";
                        else if (action.Equals("SetNull", StringComparison.OrdinalIgnoreCase))
                            action = "SET NULL";
                        else if (action.Equals("Cascade", StringComparison.OrdinalIgnoreCase))
                            action = "CASCADE";

                        onUpdateSyntax = $" ON UPDATE {action}";
                    }
                }
            }
            // Handle older format syntax that might use different formats
            else
            {
                // Format the options to SQL standard syntax
                var formattedOption = option.Replace("NoAction", "NO ACTION")
                                            .Replace("SetNull", "SET NULL");

                if (formattedOption.Contains("ON DELETE", StringComparison.OrdinalIgnoreCase) ||
                    formattedOption.Contains("ON UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    // The option already contains complete constraint syntax
                    return $@"ALTER TABLE [dbo].[{Name}] ADD CONSTRAINT [FK_{Name}_{c.Name}] FOREIGN KEY ([{c.Name}])
REFERENCES [dbo].[{fkTable}]([{cName}]) {formattedOption};
GO";
                }
                else
                {
                    // Single action name - assume it's for DELETE
                    onDeleteSyntax = $" ON DELETE {formattedOption}";
                }
            }
        }
        else
        {
            // 기본 동작 유지 - NULL 허용 여부에 따라 다른 기본값 설정
            onDeleteSyntax = c.NN == true ?
                " ON DELETE CASCADE" :
                " ON DELETE SET NULL";
        }

        // 특수 처리: Message 테이블의 Parent_id 및 ThreadRoot_id 필드는 설계서에 따라 ON DELETE NO ACTION 적용
        if (Name == "Message" && (c.Name == "Parent_id" || c.Name == "ThreadRoot_id"))
        {
            onDeleteSyntax = " ON DELETE NO ACTION";
        }

        // Add semicolon before GO
        var code = $@"ALTER TABLE [dbo].[{Name}] ADD CONSTRAINT [FK_{Name}_{c.Name}] FOREIGN KEY ([{c.Name}])
REFERENCES [dbo].[{fkTable}]([{cName}]){onDeleteSyntax}{onUpdateSyntax};
GO";
        return code;
    }

    private static string OutputColumnLine(ColumnMeta c)
    {
        var systemType = c.GetSystemType();
        var typeText = c.GetSqlType();
        if (c.GetSize() is string size)
        {
            typeText += $"({size.ToUpper()})";
        }

        var notnullText = c.NN is null || (bool)c.NN == false ? "NULL" : "NOT NULL";
        var output = $"[{c.Name}] {typeText} {notnullText}";

        string? defaultValue = c.Default;
        if (c.PK)
        {
            output += " PRIMARY KEY";
            if ((defaultValue == null && c.GetSystemType() == typeof(Guid)))
            {
                defaultValue = "NEWID()";
            }
        }

        if (defaultValue == null && c.NN != null && (bool)c.NN)
        {
            if (systemType == typeof(DateTime))
            {
                defaultValue = "GETDATE()";
            }
        }

        if (string.IsNullOrEmpty(defaultValue) != true)
        {
            if (systemType == typeof(string))
            {
                if (defaultValue.StartsWith('\"'))
                {
                    defaultValue = $"'{defaultValue.GetBetween("\"", "\"")}'";
                }
                else if (defaultValue.StartsWith('\'') != true)
                {
                    defaultValue = $"'{defaultValue}'";
                }

                if (defaultValue.StartsWith('\''))
                {
                    defaultValue = $"N{defaultValue}";
                }
            }

            if (defaultValue.Contains("@by", StringComparison.OrdinalIgnoreCase))
            {
                if (c.GetSystemType() == typeof(string))
                    defaultValue = "'@system'";
                else
                    defaultValue = null;
            }
            else if (defaultValue.Contains("@now", StringComparison.OrdinalIgnoreCase))
                defaultValue = "GETDATE()";

            else if (defaultValue.Contains("true", StringComparison.OrdinalIgnoreCase))
                defaultValue = "1";

            else if (defaultValue.Contains("false", StringComparison.OrdinalIgnoreCase))
                defaultValue = "0";

            if (string.IsNullOrEmpty(defaultValue) != true)
                output += $" DEFAULT {defaultValue}";
        }

        var comment = c.Label == c.Name ? null : c.Label;
        if (c.Comment != null)
        {
            if (comment != null)
            {
                comment += ": ";
            }
            comment += c.Comment;
        }
        else if (c.Description != null)
        {
            if (comment != null)
            {
                comment += ": ";
            }
            comment += c.Description;
        }

        if (comment != null)
        {
            output += $", -- {comment}";
        }
        else
        {
            output += ",";
        }

        return output;
    }
}