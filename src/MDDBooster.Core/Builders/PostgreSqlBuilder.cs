using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDDBooster.Builders
{
    public class PostgreSqlBuilder : BuilderBase
    {
        public PostgreSqlBuilder(ModelMetaBase m) : base(m)
        {
        }

        private static readonly Dictionary<string, Type> typeAliasToTypeMap = new()
        {
            { "bool", typeof(bool) },
            { "string", typeof(string) },
            { "int", typeof(int) },
            { "long", typeof(long) },
            { "float", typeof(float) },
            { "datetime", typeof(DateTime) },
            { "guid", typeof(Guid) },
            { "double", typeof(double) },
            { "decimal", typeof(decimal) },
            { "char", typeof(char) },
            { "byte", typeof(byte) },
            { "byte[]", typeof(byte[]) },
            { "enum", typeof(int) },
            { "money", typeof(decimal) },
        };

        private static readonly Dictionary<Type, string> TypeTotypeAliasMap = new()
        {
            { typeof(bool), "bool" },
            { typeof(string), "string" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(float), "float" },
            { typeof(DateTime), "DateTime" },
            { typeof(Guid), "Guid" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(char), "char" },
            { typeof(byte), "byte" },
            { typeof(byte[]), "byte[]" }
        };

        private static readonly Dictionary<string, Type> sqlTypeToTypeMap = new()
        {
            { "BOOLEAN", typeof(bool) },
            { "VARCHAR", typeof(string) },
            { "INTEGER", typeof(int) },
            { "BIGINT", typeof(long) },
            { "REAL", typeof(float) },
            { "TIMESTAMP", typeof(DateTime) },
            { "UUID", typeof(Guid) },
            { "DOUBLE", typeof(double) },
            { "DECIMAL", typeof(decimal) },
            { "CHAR", typeof(char) },
            { "TINYINT", typeof(byte) },
            { "BINARY", typeof(byte[]) }
        };

        private static readonly Dictionary<Type, string> typeToSqlTypeMap = new()
        {
            { typeof(bool), "BOOLEAN" },
            { typeof(string), "VARCHAR" },
            { typeof(int), "INTEGER" }, // Int32
            { typeof(long), "BIGINT" }, // Int64
            { typeof(float), "REAL" }, // Single
            { typeof(DateTime), "TIMESTAMP" },
            { typeof(Guid), "UUID" },
            { typeof(double), "DOUBLE" },
            { typeof(decimal), "DECIMAL" },
            { typeof(char), "CHAR" },
            { typeof(byte), "TINYINT" },
            { typeof(byte[]), "BINARY" },
        };

        private static readonly Dictionary<string, string> typeNameToSqlTypeMap = new()
        {
            { "money", "MONEY" },
            { "enum", "INTEGER" }
        };

        public void Build(string basePath)
        {
#if DEBUG
            if (this.meta.Name == "Plan")
            {

            }
#endif

            var columnLines = FullColumns.Select(p => OutputColumnLIne(p));
            var columnLinesText = string.Join(",\r\n\t", columnLines);

            var indexLines = FullColumns.Where(p => p.UI).Select(p => OutputIndexLine(p));
            var indexLinesText = string.Join(Constants.NewLine, indexLines);

            var fkLines = FullColumns.Where(p => p.FK).Select(p => OutputFKLine(p));
            var fkLinesText = string.Join(Constants.NewLine, fkLines);

            var uniqueLines = GetUniqueLines(out var nullableUniqueLines);
            var uniqueLinesText = string.Empty;
            if (uniqueLines != null && uniqueLines.Any())
            {
                var line = string.Join($",{Constants.NewLine}\t", uniqueLines);
                uniqueLinesText = $",{Constants.NewLine}\t{line}";
            }
            var nullableUniqueLinesText = string.Empty;
            if (nullableUniqueLines != null && nullableUniqueLines.Any())
            {
                var line = string.Join($"{Constants.NewLine}", nullableUniqueLines.Select(p => $"{p}{Constants.NewLine};"));
                nullableUniqueLinesText = $"{Constants.NewLine}{line}";
            }

            var code = $@"-- # {Constants.NO_NOT_EDIT_MESSAGE}
CREATE TABLE ""{Name}""
(
    {columnLinesText}{uniqueLinesText}
);
{nullableUniqueLinesText}";
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
                        var line = $@"CONSTRAINT ""UK_{Name}_{c.Name}"" UNIQUE (""{c.Name}"")";
                        list.Add(line);
                    }
                    else
                    {
                        var line = $@"CREATE UNIQUE INDEX ""IDX_{Name}_{c.Name}"" ON ""{Name}""(""{c.Name}"") WHERE ""{c.Name}"" IS NOT NULL";
                        nullableUniqueList.Add(line);
                    }
                }
            }

            nullableUniqueLines = nullableUniqueList.ToArray();

            var uniqueMultiples = this.meta.GetUniqueMultiples();
            if (uniqueMultiples.Any() != true) return list.ToArray();

            foreach (var uniqueMultiple in uniqueMultiples)
            {
                var nm = string.Join(string.Empty, uniqueMultiple);
                var fields = uniqueMultiple.Select(p => $@"""{p}""");
                var fieldsText = string.Join(", ", fields);
                var line = $@"CONSTRAINT ""UK_{Name}_{nm}"" UNIQUE ({fieldsText})";
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

            var option = c.GetForeignKeyOption();
            var onSyntax = option == null
                ? c.NN == true
                ? $" ON DELETE CASCADE"
                : $" ON DELETE SET NULL"
                : string.Empty;

            var code = $@"ALTER TABLE ""{Name}"" ADD CONSTRAINT ""FK_{Name}_{c.Name}"" FOREIGN KEY (""{c.Name}"")
REFERENCES ""{fkTable}""(""{cName}""){onSyntax};";
            return code;
        }

        private string OutputIndexLine(ColumnMeta c)
        {
            return $@"CREATE INDEX ""IX_{Name}_{c.Name}"" ON ""{Name}""(""{c.Name}"" ASC);";
        }

        private string OutputColumnLIne(ColumnMeta c)
        {
            var systemType = GetSystemType(c);
            var typeText = GetSqlType(c);
            if (GetSize(c) is string size)
            {
                if(size.ToUpper() != "MAX")
                    typeText += $"({size.ToUpper()})";
                else
                    typeText = $"TEXT";
            }

            var notnullText = c.NN is null || (bool)c.NN == false ? "NULL" : "NOT NULL";
            var output = typeText != "BOOLEAN" && typeText != "TIMESTAMP"
                ? $@"""{c.Name}"" {typeText} {notnullText}" 
                : $@"""{c.Name}"" {typeText}";

            string? defaultValue = c.Default;
            if (c.PK)
            {
                output += " PRIMARY KEY";
                if ((defaultValue == null && GetSystemType(c) == typeof(Guid)))
                {
                    defaultValue = "GEN_RANDOM_UUID()";
                }
            }

            if (defaultValue == null && c.NN != null && (bool)c.NN)
            {
                if (systemType == typeof(DateTime))
                {
                    defaultValue = "NOW()";
                }
            }

            if (string.IsNullOrEmpty(defaultValue) != true)
            {
                if (systemType == typeof(string))
                {
                    if (defaultValue.StartsWith("\""))
                    {
                        defaultValue = $"'{defaultValue.GetBetween("\"", "\"")}'";
                    }
                    else if (defaultValue.StartsWith("'") != true)
                    {
                        defaultValue = $"'{defaultValue}'";
                    }

                    if (defaultValue.StartsWith("'"))
                    {
                        defaultValue = $"N{defaultValue}";
                    }
                }

                if (defaultValue.Contains("@by"))
                    defaultValue = "'@system'";

                else if (defaultValue.Contains("@now"))
                    defaultValue = "NOW()";

                output += $" DEFAULT {defaultValue}";
            }

            return output;
        }

        public Type GetSystemType(ColumnMeta c)
        {
            if (typeAliasToTypeMap.TryGetValue(c.DataType.ToLower(), out var t))
            {
                if (c.DataType.ToLower() == "enum")
                {
                    if (IsEnumKey(c))
                        return typeof(string);
                    else
                        return typeof(int);
                }
                return t;
            }
            else if (sqlTypeToTypeMap.TryGetValue(c.DataType.ToUpper(), out var t2))
                return t2;

            else
                throw new NotImplementedException($"GetSystemType - {c.DataType}");
        }

        public string GetSqlType(ColumnMeta c)
        {
            if (typeNameToSqlTypeMap.TryGetValue(c.DataType.ToLower(), out var t1))
            {
                if (c.DataType.ToLower() == "enum")
                {
                    if (IsEnumKey(c))
                        return "VARCHAR";
                    else
                        return "INTEGER";
                }

                return t1;
            }
            else
            {
                var systemType = GetSystemType(c);
                if (typeToSqlTypeMap.TryGetValue(systemType, out var t))
                    return t;

                else
                    throw new NotImplementedException($"GetSqlType - {c.DataType}");
            }
        }

        internal string? GetSize(ColumnMeta c)
        {
            var Size = c.Size;

            if (Size != null)
                return Size;

            else
            {
                var systemType = GetSystemType(c);
                if (systemType == typeof(string))
                    return "50";
                else
                    return null;
            }
        }

        internal bool IsEnumKey(ColumnMeta c) =>
                    c.LineText.Contains("enum", StringComparison.OrdinalIgnoreCase)
                    && c.LineText.Contains("key:", StringComparison.OrdinalIgnoreCase);

    }
}