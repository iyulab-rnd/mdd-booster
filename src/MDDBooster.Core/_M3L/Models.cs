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
}