using MDDBooster.Models;
using System.Text.RegularExpressions;
using System.Xml;

namespace MDDBooster;

public abstract class ModelMetaBase : IModelMeta
{
    public string Name { get; }
    public string Headline { get; }
    public string Body { get; }

    public string[] Extensions { get; internal set; }
    public InterfaceMeta[]? Interfaces { get; internal set; }
    public AbstractMeta? Abstract { get; internal set; }

    public string[] InterfaceNames { get; private set; } = [];

    public string? AbstractName
    {
        get => abstractName ?? Abstract?.Name;
        private set => abstractName = value;
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
            var interfaces = new List<string>();
            foreach (var item in line.Split(","))
            {
                var itemName = item.Trim();
                if (itemName.StartsWith('@'))
                    extensions.Add(itemName);

                else if (itemName.StartsWith('I') && itemName[1] >= 'A' && itemName[1] <= 'Z')
                    interfaces.Add(itemName);

                else
                    AbstractName = itemName;
            }

            this.Extensions = [.. extensions];
            this.InterfaceNames = [.. interfaces];
        }
    }

    public virtual void Validate()
    {
        foreach (var column in this.Columns)
        {
            if (column.UQ && column.GetSystemType() == typeof(string) && column.GetMaxLength() == null)
            {
                throw new Exception($"Unique string column must have max length - {column.Name}");
            }
        }
    }

    private ColumnMeta[]? _Columns;
    private ColumnMeta[]? _FullColumns;
    private string? abstractName;

    public string Label { get; private set; }
    public ColumnMeta[] Columns => _Columns ??= BuildColumns();
    public ColumnMeta[] FullColumns => _FullColumns ??= BuildFullColumns();

    public bool IsAbstract => this is AbstractMeta;

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
            foreach (var field in fields)
            {
                var column = FullColumns.FirstOrDefault(p => p.Name == field) ?? throw new Exception($"Cannot find column - {field}");
            }
            list.Add(fields.ToArray());
        }
        return list.ToArray();
    }

    // 모든 인덱스 정보를 가져옴
    internal List<IndexMeta> GetIndexes()
    {
        var indexes = new List<IndexMeta>();

        // 개별 컬럼의 @index 속성 수집
        foreach (var column in Columns)
        {
            indexes.AddRange(column.Indexes);
        }

        // 테이블 레벨의 @index 속성 수집
        var indexMatches = Regex.Matches(this.Body, @"@index:\s*\[(.*?)\]\s*(?:\[([^\]]*)\])?");
        foreach (Match match in indexMatches)
        {
            var columns = match.Groups[1].Value.Split(',').Select(c => c.Trim()).ToList();

            var indexName = match.Groups.Count > 2 && match.Groups[2].Success
                ? match.Groups[2].Value.Trim()
                : null;

            // "IX"라는 기본 이름은 무시하고 더 구체적인 이름 생성
            if (indexName == "IX")
            {
                indexName = null;
            }

            indexes.Add(new IndexMeta
            {
                Columns = columns,
                Name = indexName
            });
        }

        return indexes;
    }

    internal bool IsDefault() => this.Headline.Contains("@default");

    internal ColumnMeta GetPKColumn()
    {
        var pkColumn = this.FullColumns.FirstOrDefault(p => p.PK);
        return pkColumn == null ? throw new Exception($"Cannot find PK Column - {this.Name}") : pkColumn;
    }
}