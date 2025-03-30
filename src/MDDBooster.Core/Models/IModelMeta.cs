namespace MDDBooster.Models;

public interface IModelMeta
{
    string Name { get; }
    string Headline { get; }
    string Body { get; }
    void Validate();
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
        return Columns.Where(p => p.FK);
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