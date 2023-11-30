
namespace M3L;

public class Entity
{
    public required string Name { get; set; }
    public string? BaseEntity { get; set; }
    public string[]? Interfaces { get; set; }
    public string? Label { get; set; }

    public EntityProperty[]? Properties { get; set; }
    public EntityOption[]? Options { get; set; }
}

public class EntityOption
{
}

public class EntityProperty
{
    public required string Name { get; set; }
    public string? DataType { get; set; }
    public Type? SystemType { get; set; }
    public bool IsNotNull { get; set; }
    public string? Label { get; set; }
    public double Length { get; set; }
    public bool IsUnique { get; set; }
    public bool UseIndex { get; set; }
    public string? DefaultValue { get; set; }
    public EntityForeignKey? ForeignKey { get; set; }
}

public class EntityForeignKey
{
    public string? Entity { get; set; }
    public string? Property { get; set; }
}