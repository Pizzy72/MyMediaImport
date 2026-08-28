namespace MyMediaImport.Core;

public sealed class ImportPlan
{
    public ImportPlan(IReadOnlyList<ImportPlanItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items;
    }

    public IReadOnlyList<ImportPlanItem> Items { get; }
}
