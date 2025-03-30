namespace MDDBooster.Builders;

public abstract class BuilderBase
{
    protected readonly ModelMetaBase meta;

    public BuilderBase(ModelMetaBase m)
    {
        meta = m;
    }

    public string Name => meta.Name;
    public ColumnMeta[] Columns => meta.Columns;
    public ColumnMeta[] FullColumns => meta.FullColumns;
}