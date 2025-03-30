namespace M3LParser.Models;

public class M3LField
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool IsNullable { get; set; }
    public string Length { get; set; }
    public string Description { get; set; }
    public string DefaultValue { get; set; }
    public List<string> Attributes { get; set; } = new List<string>();
    public List<string> FrameworkAttributes { get; set; } = new List<string>();
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    public bool IsPrimaryKey => Attributes.Any(a => a.StartsWith("@primary"));
    public bool IsUnique => Attributes.Any(a => a.StartsWith("@unique"));
    public bool IsRequired => !IsNullable;
    public bool IsReference => Attributes.Any(a => a.StartsWith("@reference"));
    public string ReferenceTarget => GetAttributeValue("@reference");

    private string GetAttributeValue(string attributeName)
    {
        var attribute = Attributes.FirstOrDefault(a => a.StartsWith(attributeName));
        if (attribute == null) return null;

        var match = Regex.Match(attribute, $@"{attributeName}\(([^)]+)\)");
        return match.Success ? match.Groups[1].Value : null;
    }
}
