using System.Text.RegularExpressions;

namespace MDDBooster.Models;

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
    public bool UI { get; private set; } // use index (통합된 인덱스 플래그)
    public bool? NN { get; private set; } // not null
    public string? Size { get; private set; }
    public string? Default { get; private set; }
    public IEnumerable<AttributeMeta> Attributes { get; private set; }
    public string LineText { get; private set; }
    public string Label { get; }
    public string ShortName { get; }
    public string? Description { get; private set; }
    public string? Comment { get; private set; }
    public bool IsNullable => NN == false;
    public List<IndexMeta> Indexes { get; private set; } = new List<IndexMeta>();

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
            this.ShortName = label;
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
            if (line.Contains("enum") && defaultText.Contains('|') && defaultText.Contains('='))
            {
                // default 값이 아님 (enum 의 속성정보임)
            }
            else
            {
                defaultText = defaultText.LeftOr("[").LeftOr("//").LeftOr("/*");
                this.Default = defaultText.Trim();
            }
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
        ParseIndex(lineText);
        ParseComments(lineText);

        if (this.IsNotNull() && string.IsNullOrWhiteSpace(this.Default))
        {
            if (this.Attributes != null && this.Attributes.FirstOrDefault(p => p.Name == "Insert") is AttributeMeta insertAttr)
            {
                if (this.GetSystemType() == typeof(string))
                {
                    this.Default = insertAttr.Value;
                }
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

            // Filter and process attributes during parsing
            foreach (var attr in items)
            {
                // Process special attributes and set flags
                if (string.Equals(attr.Name, "FK", StringComparison.OrdinalIgnoreCase))
                {
                    this.FK = true;
                    // Don't add FK attributes with colon syntax to the collection
                    if (!attr.Line.StartsWith("FK:", StringComparison.OrdinalIgnoreCase))
                    {
                        attributes.Add(attr);
                    }
                }
                else if (string.Equals(attr.Name, "desc", StringComparison.OrdinalIgnoreCase))
                {
                    this.Description = attr.Value;
                    // Don't add desc attributes to the collection
                }
                else if (string.Equals(attr.Name, "UK", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(attr.Name, "UQ", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(attr.Name, "Unique", StringComparison.OrdinalIgnoreCase))
                {
                    this.UQ = true;
                    // Don't add UK/UQ attributes to the collection
                }
                else if (string.Equals(attr.Name, "UI", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(attr.Name, "IDX", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(attr.Name, "Index", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(attr.Name, "IX", StringComparison.OrdinalIgnoreCase))
                {
                    this.UI = true;
                    // Don't add UI/IDX/IX attributes to the collection
                }
                else if (string.Equals(attr.Name, "PK", StringComparison.OrdinalIgnoreCase))
                {
                    this.PK = true;
                    this.NN = true;
                    // Don't add PK attributes to the collection
                }
                else
                {
                    // Add all other attributes to the collection
                    attributes.Add(attr);
                }
            }
        }

        if (this.Description == null && lineText.Contains('#'))
        {
            this.Description = lineText.Right("#").LeftOr("//").LeftOr("/*").Trim();
        }

        this.Attributes = attributes;
    }

    private void ParseComments(string lineText)
    {
        // Parse block comments /* */
        var blockCommentMatch = Regex.Match(lineText, @"/\*(.*?)\*/");
        if (blockCommentMatch.Success)
        {
            this.Comment = blockCommentMatch.Groups[1].Value.Trim();
        }
        // Parse line comments //
        else if (lineText.Contains("//"))
        {
            this.Comment = lineText.Right("//").Trim();
        }
    }

    private void ParseIndex(string lineText)
    {
        // 단일 컬럼 인덱스를 위한 @index 구문 파싱
        var singleIndexMatches = Regex.Matches(lineText, @"@index:\s*\[(.*?)\]\s*(?:\[([^\]]*)\])?");
        foreach (Match match in singleIndexMatches)
        {
            var columnName = match.Groups[1].Value.Trim();

            // 현재 컬럼을 참조하는 경우만 처리
            if (columnName == this.Name)
            {
                // 인덱스 이름이 "IX"인 경우 null로 설정하여 자동 이름 생성 로직 사용
                var indexName = match.Groups.Count > 2 && match.Groups[2].Success
                    ? match.Groups[2].Value.Trim()
                    : null;

                if (indexName == "IX")
                {
                    indexName = null;
                }

                Indexes.Add(new IndexMeta
                {
                    Columns = new List<string> { this.Name },
                    Name = indexName
                });
            }
        }
    }

    internal bool IsNotNull() => this.NN == true;

    private IEnumerable<string> GetAttributeLines(string lineText)
    {
        var line = Functions.GetConentLine(lineText);
        while (line.Contains('[') && line.Contains(']'))
        {
            var startIndex = line.IndexOf('[');
            var endIndex = line.IndexOf(']');
            var attributeString = line.Substring(startIndex + 1, endIndex - startIndex - 1);
            line = line.Remove(startIndex, endIndex - startIndex + 1);

            // "PK, Without" 이것은 2개로 분할되어야 합니다.
            // "PK(123,456), Without(44,55) 이것은 2개로 분할되어야 합니다.
            var attributes = attributeString.Split(new[] { ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var attribute in attributes)
            {
                yield return attribute.Trim();
            }
        }
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
            else if (option.Equals("UI", StringComparison.OrdinalIgnoreCase) ||
                     option.Equals("IDX", StringComparison.OrdinalIgnoreCase) ||
                     option.Equals("index", StringComparison.OrdinalIgnoreCase) ||
                     option.Equals("IX", StringComparison.OrdinalIgnoreCase))
                this.UI = true;
        }
    }

    internal bool IsEnumType() => DataType.Equals("enum", StringComparison.OrdinalIgnoreCase);
    internal bool IsEnumKey() =>
        LineText.Contains("enum", StringComparison.OrdinalIgnoreCase)
        && LineText.Contains("key:", StringComparison.OrdinalIgnoreCase)
        && LineText.GetBetween("key:", "|").Contains('=') != true; // = 기호가 있으면 대입되는 값이 있으므로 Key로 저장하지 않음

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
            if (optionsText.Contains("name:"))
            {
                var contains = optionsText.GetBetween("name:", ",", false, true);
                optionsText = optionsText.Replace(contains, string.Empty);
            }
            if (optionsText.Contains("key:"))
            {
                optionsText = optionsText.Replace("key:", string.Empty).Trim();
            }
            var keyOptions = optionsText.RightOr("enum:").LeftOr(",").LeftOr(")");
            var options = keyOptions.Split("|");
            return [.. options];
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
            var name = s.LeftOr(",");
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
