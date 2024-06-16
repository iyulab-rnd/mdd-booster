using System.Text.RegularExpressions;
using System.Xml.Linq;

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
        public static string[] GetInterfaceOrInherits(this IModelMeta meta)
        {
            return meta.Headline
                .Right(":")
                .Split(",")
                .Where(p => p.Length > 0)
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

        public string[] Extensions { get; internal set; }
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

            if (headline.GetBetween("(", ")") is string s && s.Length > 0)
            {
                Label = s.Trim();
            }

            if (headline.Right(":") is string right && right.Length > 0)
            {
                var line = Functions.GetConentLine(right);
                var extensions = new List<string>();
                
                foreach(var item in line.Split(","))
                {
                    var itemName = item.Trim();
                    if (itemName.StartsWith('@'))
                        extensions.Add(itemName);
                }

                this.Extensions = [.. extensions];
            }
        }

        private ColumnMeta[]? _Columns;
        private ColumnMeta[]? _FullColumns;
        private string? abstractName;

        public string Label { get; private set; }
        public ColumnMeta[] Columns => _Columns ??= BuildColumns();
        public ColumnMeta[] FullColumns => _FullColumns ??= BuildFullColumns();

        protected virtual ColumnMeta[] BuildColumns()
        {
            var list = new List<ColumnMeta>();

            foreach (Match m in Regex.Matches(Body, @"\-\s+\w+.*").Cast<Match>())
            {
                var c = new ColumnMeta(m.Value);
                list.Add(c);
            }

            return list.ToArray();
        }

        protected virtual ColumnMeta[] BuildFullColumns()
        {
            var list = new List<ColumnMeta>();

            foreach (Match m in Regex.Matches(Body, @"\-\s+\w+.*").Cast<Match>())
            {
                var c = new ColumnMeta(m.Value);
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
                foreach(var field in fields)
                {
                    var column = Columns.FirstOrDefault(p => p.Name == field) ?? throw new Exception($"Cannot find column - {field}");
                }
                list.Add(fields.ToArray());
            }
            return list.ToArray();
        }

        internal bool IsDefault() => this.Headline.Contains("@default");

        internal ColumnMeta GetPKColumn()
        {
            var pkColumn = this.FullColumns.FirstOrDefault(p => p.PK);
            return pkColumn == null ? throw new Exception($"Cannot find PK Column - {this.Name}") : pkColumn;
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
            { "guid", typeof(Guid) },
            { "datetime", typeof(DateTime) },
            { "date", typeof(DateOnly) },
            { "time", typeof(TimeOnly) },
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
            { typeof(Guid), "Guid" },
            { typeof(DateTime), "DateTime" },
            { typeof(DateOnly), "DateOnly" },
            { typeof(TimeOnly), "TimeOnly" },
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
            { "UNIQUEIDENTIFIER", typeof(Guid) },
            { "DATETIME2", typeof(DateTime) },
            { "DATE", typeof(DateOnly) },
            { "TIME", typeof(TimeOnly) },
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
            { typeof(Guid), "UNIQUEIDENTIFIER" },
            { typeof(DateTime), "DATETIME2" },
            { typeof(DateOnly), "DATE" },
            { typeof(TimeOnly), "TIME" },
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
        public bool PK { get; private set; } // primary key
        public bool FK { get; private set; } // foreign key
        public bool UQ { get; private set; } // unique
        public bool UI { get; private set; } // use index
        public bool? NN { get; private set; } // not null
        public string? Size { get; private set; }
        public string? Default { get; private set; }
        public IEnumerable<AttributeMeta> Attributes { get; private set; }
        public string LineText { get; private set; }
        public string Label { get; }
        public string ShortName { get; }
        public string? Description { get; private set; }

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

        internal int? GetMaxLength()
        {
            if (GetSize() is string size)
            {
                if (int.TryParse(size, out var length))
                    return length;
                else
                    return null;
            }
            else
                return null;
        }

        public ColumnMeta(string lineText)
        {
            var line = Functions.GetConentLine(lineText);
            var nameText = line.GetBetween("-", ":").Trim();
            var name = nameText;

#if DEBUG
            if (name.StartsWith("ConsumerPrice"))
            {
            }
#endif
            string label;
            if (name.Contains('('))
            {
                label = name.GetBetweenBlock("(", ")").Trim();
                name = name.LeftOr("(").Trim();
            }
            else
            {
                label = name.EndsWith('?') ? name[..^1] : name;
            }
            if (label.Contains(','))
            {
                this.Label = label.LeftOr(",").Trim();
                this.ShortName = label.RightOr(",").Trim();
            }
            else
            {
                this.Label = label;
            }
            
            if (name.EndsWith('?'))
            {
                this.NN = false;
                name = name[..^1];
            }
            else
            {
                this.NN = true;
            }

            var dataType = line.RegexReturn(@"\:\s+(\w+(\?|))", 1) ?? throw new Exception($"Cannot Parse DataType, {lineText}");
            if (dataType.EndsWith('?'))
            {
                NN = false;
                dataType = dataType[..^1];
            }

            LineText = lineText;
            Name = name;
            DataType = dataType;

            if (line.RegexReturn(@"\s*=\s*(.*)?", 1) is string defaultText)
            {
                defaultText = defaultText.LeftOr("[").LeftOr("//");
                this.Default = defaultText.Trim();
            }

            if (DataType != "enum" && line.GetBetween($"{DataType}(", ")") is string s && s.Length > 0)
            {
                if (int.TryParse(s, out var size))
                    Size = size.ToString();

                else if (s.Equals("max", StringComparison.OrdinalIgnoreCase))
                    Size = "max";

                else if (Regex.IsMatch(s, @"[0-9,]+"))
                    Size = s;
            }

            ParseOptions(lineText);
            ParseAttribtes(lineText);

            if (this.IsNotNull() && string.IsNullOrWhiteSpace(this.Default))
            {
                if (this.Attributes != null && this.Attributes.FirstOrDefault(p => p.Name == "Insert") is AttributeMeta insertAttr)
                {
                    this.Default = insertAttr.Value;
                }
            }

            if (this.FK != true && this.Name.StartsWith('_') != true && (this.Name.EndsWith("_id") || this.Name.EndsWith("_key")))
            {
                var fkEntityName = this.Name.Left("_");
                if (Resolver.Models!.Any(p => p.Name == fkEntityName))
                {
                    FK = true;
                }
                else
                {
                    // 존재하지 않는 Entity
                }
            }
        }

        private void ParseAttribtes(string lineText)
        {
            var attributes = new List<AttributeMeta>();

            var m = Regex.Matches(lineText, @"\[(.*?(\(.*?\))?.*?)\]");
            foreach (Match match in m.Cast<Match>())
            {
                var line = match.Groups[1].Value;
                var items = line.Split(new[] { "],[" }, StringSplitOptions.None)
                                .Select(p => p.Trim('[', ']'))
                                .Select(p => AttributeMeta.Build(p));
                attributes.AddRange(items);
            }

            foreach (var attribute in attributes)
            {
                if (string.Equals(attribute.Name, "FK", StringComparison.OrdinalIgnoreCase))
                    this.FK = true;
                else if (string.Equals(attribute.Name, "desc", StringComparison.OrdinalIgnoreCase))
                    this.Description = attribute.Value;
            }

            if (this.Description == null && lineText.Contains('#'))
            {
                this.Description = lineText.Right("#").LeftOr("//").Trim();
            }

            this.Attributes = attributes;
        }

        internal bool IsNotNull() => this.NN == true;

        private IEnumerable<string> GetAttributeLines(string lineText)
        {
            var line = Functions.GetConentLine(lineText);
            var contents = line.GetBetween("[", "]");
            return contents.Split(",").Select(p => p.Trim());
        }

        private void ParseOptions(string lineText)
        {
            var attributeLines = GetAttributeLines(lineText);

            foreach (var attributeLine in attributeLines)
            {
                var option = attributeLine;
                if (option.Equals("PK", StringComparison.OrdinalIgnoreCase))
                {
                    this.PK = true;
                    this.NN = true;
                }
                else if (option.StartsWith("FK", StringComparison.OrdinalIgnoreCase))
                    this.FK = true;

                else if (option.Equals("UQ", StringComparison.OrdinalIgnoreCase) || option.Equals("unique", StringComparison.OrdinalIgnoreCase))
                    this.UQ = true;

                else if (option.Equals("UI", StringComparison.OrdinalIgnoreCase) || option.Equals("IDX", StringComparison.OrdinalIgnoreCase) || option.Equals("index", StringComparison.OrdinalIgnoreCase))
                    this.UI = true;
            }
        }

        internal bool IsEnumType() => DataType.Equals("enum", StringComparison.OrdinalIgnoreCase);
        internal bool IsEnumKey() => 
            LineText.Contains("enum", StringComparison.OrdinalIgnoreCase)
            && LineText.Contains("key:", StringComparison.OrdinalIgnoreCase);

        internal bool IsEnumValue() => !IsEnumKey();

        internal string GetEnumTypeName()
        {   
            var name = this.LineText.Right("name:").LeftOr(",").LeftOr(")").Trim();
            if (string.IsNullOrEmpty(name))
            {
                name = this.Name.ToPlural();
            }
            return name;
        }

        internal string[]? GetEnumOptions()
        {
            var optionsText = LineText.RegexReturn("enum\\((.*?)\\)", 1);
            if (optionsText != null)
            {
                var keyOptions = optionsText.RightOr("enum:").LeftOr(",").LeftOr(")");
                var options = keyOptions.Split("|").Select(p =>
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
            string ret;
            if (LineText.Contains("FK:"))
            {
                var s = LineText.GetBetween("FK:", "]");
                var name =  s.LeftOr(",");
                ret = name.Contains('.') ? name.Left(".") : name.LeftOr("_");
            }
            else if (Name.Contains('_'))
                ret = Name.Left("_");

            else
                throw new NotImplementedException();

            return ret.Trim();
        }

        internal string GetForeignKeyColumnName()
        {
            string ret;
            if (LineText.Contains("FK:"))
            {
                var s = LineText.GetBetween("FK:", "]");
                var p = s.LeftOr(",");
                ret = p.Contains('.') ? p.Right(".") : Name.Right("_");
            }
            else if (Name.Contains('_'))
                ret = Name.Right("_");

            else
                throw new NotImplementedException();

            return ret.Trim();
        }

        internal string? GetForeignKeyOption()
        {
            if (LineText.Contains("FK:"))
            {
                var s = LineText.GetBetween("FK:", "]");
                return s.Contains(',') ? s.Right(",").Trim() : null;
            }
            else
                return null;
        }
    }

    public class AttributeMeta
    {
        public required string Name { get; set; }
        public string? Value { get; set; }
        public required string Line { get; set; }

        internal static AttributeMeta Build(string line)
        {
            string name;
            string? value = null;

            if (line.Contains('('))
            {
                name = line.Left("(").Trim();
                value = line.GetBetween("(", ")").Trim();
            }
            else if (line.Contains(':'))
            {
                name = line.Left(":").Trim();
                value = line.Right(":").Trim();
            }
            else
            {
                name = line;
            }

            return new AttributeMeta()
            {
                Name = name,
                Value = value,
                Line = line
            };
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

            var name = headline.RegexReturn(@"\#\#\s+(\w+)", 1);

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