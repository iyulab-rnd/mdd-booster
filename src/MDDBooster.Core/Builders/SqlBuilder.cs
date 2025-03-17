﻿namespace MDDBooster.Builders;

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

        var indexLines = FullColumns.Where(p => p.UI).Select(p => OutputIndexLine(p));
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
            var nm = string.Join(string.Empty, uniqueMultiple);
            var fields = uniqueMultiple.Select(p => $"[{p}] ASC");
            var fieldsText = string.Join(", ", fields);
            var line = $"CONSTRAINT [UK_{Name}_{nm}] UNIQUE NONCLUSTERED ({fieldsText})";
            list.Add(line); 
        }

        return list.ToArray();
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

        // Add semicolon before GO
        var code = $@"ALTER TABLE [dbo].[{Name}] ADD CONSTRAINT [FK_{Name}_{c.Name}] FOREIGN KEY ([{c.Name}])
REFERENCES [dbo].[{fkTable}]([{cName}]){onDeleteSyntax}{onUpdateSyntax};
GO";
        return code;
    }

    private string OutputIndexLine(ColumnMeta c)
    {
        return $@"CREATE NONCLUSTERED INDEX [IX_{Name}_{c.Name}] ON [{Name}]([{c.Name}] ASC)
GO";
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
                defaultValue = "'@system'";

            else if (defaultValue.Contains("@now", StringComparison.OrdinalIgnoreCase))
                defaultValue = "GETDATE()";

            else if (defaultValue.Contains("true", StringComparison.OrdinalIgnoreCase))
                defaultValue = "1";

            else if (defaultValue.Contains("false", StringComparison.OrdinalIgnoreCase))
                defaultValue = "0";

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