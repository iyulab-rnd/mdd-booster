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
        public static string[] GetInherits(this IModelMeta meta)
        {
            return meta.Headline
                .Right(":")
                .Split(",")
                .Select(p => p.Trim())
                .Where(n => n[0] >= 'A' && n[0] <= 'Z')
                .ToArray();
        }
    }

    public abstract class ModelMetaBase : IModelMeta
    {
        public string Name { get; }
        public string Headline { get; }
        public string Body { get; }

        public InterfaceMeta[]? Interfaces { get; internal set; }
        public AbstractMeta? Abstract { get; internal set; }
        public string? AbstractName 
        { 
            get => abstractName ?? Abstract?.Name; 
            set => abstractName = value; 
        }

        protected ModelMetaBase(string name, string headline, string body)
        {
            Headline = headline;
            Body = body;
            Name = name;
        }

        private ColumnMeta[]? _Columns;
        private ColumnMeta[]? _FullColumns;
        private string? abstractName;

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

            var allNames = list.Select(p => p.Name);
            var interfaceColumns = Interfaces?.SelectMany(p => p.FullColumns);
            var abstractColumns = Abstract?.FullColumns;

            var items = list.AsEnumerable();

            if (interfaceColumns != null)
                items = items.Concat(interfaceColumns.Where(p => allNames.Contains(p.Name) != true));

            if (abstractColumns != null)
                items = items.Concat(abstractColumns.Where(p => allNames.Contains(p.Name) != true));

            return items.ToArray();
        }

        internal string[][] GetUniqueMultiples()
        {
            var matches = Regex.Matches(this.Body, @"\-\s*\@unique\:\s*(.*)(?:\r|$)");
            var line = string.Join(string.Empty, matches.Select(p => p.Groups[1].Value));
            var m_values = Regex.Matches(line, @"\((.*?)\)");
            var values = m_values.Select(p => p.Groups[1].Value);
            var list = new List<string[]>();

            foreach (var value in values)
            {
                var fields = value.Split(",").Select(p => p.Trim());
                list.Add(fields.ToArray());
            }
            return list.ToArray();
        }

        internal bool IsDefault() => this.Headline.Contains("@default");

        internal ColumnMeta GetPKColumn()
        {
            return this.FullColumns.First(p => p.PK);
        }
    }

    public class InterfaceMeta : ModelMetaBase
    {
        public InterfaceMeta(string name, string headline, string body) : base(name, headline, body)
        {
        }
    }

    public class AbstractMeta : ModelMetaBase
    {
        public AbstractMeta(string name, string headline, string body) : base(name, headline, body)
        {
        }
    }

    public class TableMeta : ModelMetaBase
    {
        public TableMeta(string name, string headline, string body) : base(name, headline, body)
        {
        }

        public IEnumerable<TableMeta> GetChildren()
        {
            return Functions.FindChildren(this);
        }

        internal IEnumerable<ColumnMeta> GetFkColumns()
        {
            return this.Columns.Where(p => p.FK);
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
            { "enum", "INTEGER" }
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
                        return "INTEGER";
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
            if (name == "OwnerKey")
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

            if (this.FK != true && this.Name.StartsWith("_") != true && (this.Name.EndsWith("_id") || this.Name.EndsWith("_key")))
                this.FK = true;

            this.LineText = lineText;
        }

        private void ParseAttribtes(string lineText)
        {
            var attributes = new List<string>();

            if (this.PK)
            {
                attributes.Add("[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
            }

            if (this.IsNotNull())
            {
                attributes.Add("[Required]");
            }

            var m = Regex.Matches(lineText, @"\[.*?\]");
            if (m.Any())
            {
                attributes.AddRange(m.Select(p => p.Value));
            }

            this.Attributes = attributes.ToArray();
        }

        internal bool IsNotNull() => this.NN == true;

        private void ParseOptions(string lineText)
        {
            //if (Name == "NormalizedEmail") Console.WriteLine(1);
    
            var text = lineText.RightFromFirst(":");
            text = text.Left("//");

            foreach (Match match in Regex.Matches(text, @"\((.*?)\)"))
            {
                var option = match.Groups[1].Value;
                if (option.Equals("PK", StringComparison.OrdinalIgnoreCase))
                    this.PK = true;

                else if (option.StartsWith("FK", StringComparison.OrdinalIgnoreCase))
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

        internal string GetForeignKeyEntityName()
        {
            if (LineText.Contains("FK:"))
            {
                var s = LineText.GetBetween("FK:", ")");
                return s.Contains(',') ? s.Left(",") : s;
            }
            else if (Name.Contains('_'))
                return Name.Left("_");

            else
                throw new NotImplementedException();
        }

        internal string? GetForeignKeyOption()
        {
            if (LineText.Contains("FK:"))
            {
                var s = LineText.GetBetween("FK:", ")");
                return s.Contains(',') ? s.Right(",") : null;
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

            else if (Utils.IsAbstract(headline))
                model = new AbstractMeta(name, headline, body);

            else
                model = new TableMeta(name, headline, body);

            return model;
        }
    }

}