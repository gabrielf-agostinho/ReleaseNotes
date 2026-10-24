namespace ReleaseNotes.Core.Catalog;

// Independent of publishing: a deprecated product can still have a published page.
public enum ProductLifecycle
{
    Active,
    Beta,
    Maintenance,
    Deprecated,
}

public static class ProductLifecycles
{
    public static IReadOnlyList<string> Labels { get; } = Enum.GetNames<ProductLifecycle>();

    public static ProductLifecycle ParseOrDefault(string? label) =>
        Enum.TryParse(label?.Trim(), ignoreCase: true, out ProductLifecycle lifecycle) && Enum.IsDefined(lifecycle)
            ? lifecycle
            : ProductLifecycle.Active;
}
