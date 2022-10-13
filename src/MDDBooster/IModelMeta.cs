using System.ComponentModel;
using System.Text.RegularExpressions;

namespace MDDBooster
{
    public interface IModelMeta
    {
        string Name { get; }
        string Headline { get; }
        string Body { get; }
    }

    public static class ModelMetaExtensions
    {
        public static string[] GetInterfaceNames(this IModelMeta meta)
        {
            return meta.Headline
                .Right(":")
                .Split(",")
                .Select(p => p.Trim())
                .Where(n => Utils.IsInterfaceName(n))
                .ToArray();
        }
    }

    public abstract class ModelMetaBase : IModelMeta
    {
        public string Name { get; }
        public string Headline { get; }
        public string Body { get; }

        public InterfaceMeta[]? Interfaces { get; internal set; }

        protected ModelMetaBase(string name, string headline, string body)
        {
            Headline = headline;
            Body = body;
            Name = name;
        }

        private ColumnMeta[]? _Columns;
        private ColumnMeta[]? _FullColumns;
        public ColumnMeta[] Columns => _Columns ??= BuildColumns();
        public ColumnMeta[] FullColumns => _FullColumns ??= BuildFullColumns();

        protected virtual ColumnMeta[] BuildColumns()
        {
#if DEBUG
            if (Name == "Plan")
            {

            }
#endif
            var list = new List<ColumnMeta>();

            foreach (Match m in Regex.Matches(Body, @"\-\s*(\w+).*?\:\s*(\w+(\?|)).*").Cast<Match>())
            {
                var name = m.Groups[1].Value;
                var dataType = m.Groups[2].Value;

                var c = new ColumnMeta(name, dataType, m.Value);
                list.Add(c);
            }

            return list.ToArray();
        }

        protected virtual ColumnMeta[] BuildFullColumns()
        {
            var list = new List<ColumnMeta>();

            foreach (Match m in Regex.Matches(Body, @"\-\s*(\w+).*?\:\s*(\w+(\?|)).*").Cast<Match>())
            {
                var name = m.Groups[1].Value;
                var dataType = m.Groups[2].Value;

                var c = new ColumnMeta(name, dataType, m.Value);
                list.Add(c);
            }

            if (Interfaces == null || Interfaces.Any() != true)
                return list.ToArray();
            else
            {
                var baseColumns = Interfaces.SelectMany(p => p.FullColumns);
                return list.Concat(baseColumns).ToArray();
            }
        }

        internal string[][] GetUniqueMultiples()
        {
            var matches = Regex.Matches(this.Body, @"\-\s*\@unique\:\s*(.*)(?:\r|$)");
            var line = string.Join(string.Empty, matches.Select(p => p.Groups[1].Value));
            var m_values = Regex.Matches(line, @"\((.*?)\)");
            var values = m_values.Select(p => p.Groups[1].Value);
            var list = new List<string[]>();

            foreach(var value in values)
            {
                var fields = value.Split(",").Select(p => p.Trim());
                list.Add(fields.ToArray());
            }
            return list.ToArray();
        }
    }

    public class InterfaceMeta : ModelMetaBase
    {
        public InterfaceMeta(string name, string headline, string body) : base(name, headline, body)
        {
        }

        internal bool IsDefault()
        {
            return this.Headline.Contains("@default");
        }
    }

    public class TableMeta : ModelMetaBase
    {
        public TableMeta(string name, string headline, string body) : base(name, headline, body)
        {
        }
    }

    public class ColumnMeta
    {
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
            { "BIT", typeof(bool) },
            { "NVARCHAR", typeof(string) },
            { "INTEGER", typeof(int) },
            { "BIGINT", typeof(long) },
            { "REAL", typeof(float) },
            { "DATETIME2", typeof(DateTime) },
            { "UNIQUEIDENTIFIER", typeof(Guid) },
            { "DOUBLE", typeof(double) },
            { "DECIMAL", typeof(decimal) },
            { "CHAR", typeof(char) },
            { "TINYINT", typeof(byte) },
            { "BINARY", typeof(byte[]) }
        };

        private static readonly Dictionary<Type, string> typeToSqlTypeMap = new()
        {
            { typeof(bool), "BIT" },
            { typeof(string), "NVARCHAR" },
            { typeof(int), "INTEGER" }, // Int32
            { typeof(long), "BIGINT" }, // Int64
            { typeof(float), "REAL" }, // Single
            { typeof(DateTime), "DATETIME2" },
            { typeof(Guid), "UNIQUEIDENTIFIER" },
            { typeof(double), "DOUBLE" },
            { typeof(decimal), "DECIMAL" },
            { typeof(char), "CHAR" },
            { typeof(byte), "TINYINT" },
            { typeof(byte[]), "BINARY" },
        };

        private static readonly Dictionary<string, string> typeNameToSqlTypeMap = new()
        {
            { "money", "MONEY" },
            { "enum", "TINYINT" }
        };

        public string Name { get; }
        public string DataType { get; }
        public bool PK { get; set; } // primary key
        public bool FK { get; set; } // foreign key
        public bool UQ { get; set; } // unique
        public bool UI { get; set; } // use index
        public bool? NN { get; set; } // not null
        public string? Size { get; set; }
        public string? Default { get; set; }
        public string[]? Attributes { get; set; }
        public string LineText { get; set; }

        public Type GetSystemType()
        {
            if (typeAliasToTypeMap.TryGetValue(DataType.ToLower(), out var t))
            {
                if (DataType.ToLower() == "enum")
                {
                    if (IsEnumKey())
                        return typeof(string);
                    else
                        return typeof(int);
                }
                return t;
            }
            else if (sqlTypeToTypeMap.TryGetValue(DataType.ToUpper(), out var t2))
                return t2;

            else
                throw new NotImplementedException($"GetSystemType - {DataType}");
        }

        public string GetSqlType()
        {
            if (typeNameToSqlTypeMap.TryGetValue(DataType.ToLower(), out var t1))
            {
                if (DataType.ToLower() == "enum")
                {
                    if (IsEnumKey())
                        return "NVARCHAR";
                    else
                        return "TINYINT";
                }

                return t1;
            }
            else
            {
                var systemType = GetSystemType();
                if (typeToSqlTypeMap.TryGetValue(systemType, out var t))
                    return t;

                else
                    throw new NotImplementedException($"GetSqlType - {DataType}");
            }
        }

        internal string GetSystemTypeAlias()
        {
            var type = GetSystemType();
            if (TypeTotypeAliasMap.TryGetValue(type, out var alias))
                return alias;

            else
                throw new NotImplementedException($"GetSystemTypeAlias - {type.Name}");
        }

        internal string? GetSize()
        {
            if (Size != null) 
                return Size;

            else
            {
                var systemType = GetSystemType();
                if (systemType == typeof(string))
                    return "50";
                else
                    return null;
            }
        }

        public ColumnMeta(string name, string dataType, string lineText)
        {
#if DEBUG
            if (name == "PlanType")
            {

            }
#endif
            Name = name;

            if (dataType.EndsWith("?"))
            {
                this.NN = false;
                DataType = dataType[..(dataType.Length - 1)];
            }
            else if (lineText.Contains(name + "?"))
            {
                this.NN = false;
                DataType = dataType;
            }
            else
            {
                this.NN = true;
                DataType = dataType;
            }

            if (lineText.GetBetween($"{DataType}(", ")") is string s)
            {
                if (int.TryParse(s, out var size))
                    Size = size.ToString();

                else if (s.Equals("max", StringComparison.OrdinalIgnoreCase))
                    Size = "max";
            }

            ParseOptions(lineText);
            ParseAttribtes(lineText);

            this.LineText = lineText;
        }

        private void ParseAttribtes(string lineText)
        {
            var attributes = new List<string>();

            if (this.IsNotNull())
            {
                attributes.Add("[Required]");
            }

            var m = Regex.Matches(lineText, @"\[[\(\)\w\s\=\""\.]+\]");
            if (m.Any())
            {
                attributes.AddRange(m.Select(p => p.Value));
            }

            this.Attributes = attributes.ToArray();
        }

        private bool IsNotNull() => this.NN == true;

        private void ParseOptions(string lineText)
        {
            //if (Name == "NormalizedEmail") Console.WriteLine(1);
    
            var text = lineText.RightFromFirst(":");
            text = text.Left("//");
            var optionsText = text.GetBetween("(", ")", true);

            if (string.IsNullOrEmpty(optionsText)) return;

            var options = optionsText.Split(",").Select(p => p.Trim());

            foreach (var option in options)
            {
                if (option.Equals("PK", StringComparison.OrdinalIgnoreCase))
                    this.PK = true;

                else if (option.Equals("FK", StringComparison.OrdinalIgnoreCase))
                    this.FK = true;

                else if (option.Equals("UQ", StringComparison.OrdinalIgnoreCase) || option.Equals("unique", StringComparison.OrdinalIgnoreCase))
                    this.UQ = true;

                else if (option.Equals("UI", StringComparison.OrdinalIgnoreCase) || option.Equals("index", StringComparison.OrdinalIgnoreCase))
                    this.UI = true;
            }
        }

        internal bool IsEnumType() => DataType.Equals("enum", StringComparison.OrdinalIgnoreCase);
        internal bool IsEnumKey() => LineText.Contains("enum(key:", StringComparison.OrdinalIgnoreCase);
        internal bool IsEnumValue() => !IsEnumKey();

        internal string[]? GetEnumOptions()
        {
            var optionsText = LineText.RegexReturn("enum\\((.*?)\\)", 1);
            if (optionsText != null)
            {
                var options = optionsText.Split("|").Select(p =>
                {
                    return p.Contains(':') ? p.Right(":").Trim() : p.Trim();
                });
                return options.ToArray();
            }
            else
                return null;
        }
    }

    public static class ModelMetaFactory
    {
        internal static IModelMeta? Create(string text)
        {
            var ndxHeadline = text.IndexOf("\r\n");
            if (ndxHeadline < 0) ndxHeadline = text.Length;

            var headline = text[..ndxHeadline];
            var body = text[ndxHeadline..];

            var name = headline.RegexReturn(@"\#\s+(\w+)", 1);

            if (string.IsNullOrEmpty(name)) return null;

            IModelMeta model;
            if (Utils.IsInterfaceName(name))
                model = new InterfaceMeta(name, headline, body);

            else
                model = new TableMeta(name, headline, body);

            return model;
        }
    }
}