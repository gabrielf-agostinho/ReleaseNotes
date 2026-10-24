namespace ReleaseNotes.Core.Catalog;

public static class ReleaseOrdering
{
    // Newest date first, then highest version (two releases can ship on the same day),
    // then product name so the order is stable.
    public static IComparer<Release> NewestFirst { get; } = Comparer<Release>.Create((x, y) =>
    {
        var result = y.ReleaseDate.CompareTo(x.ReleaseDate);
        if (result == 0)
        {
            result = y.Version.CompareTo(x.Version);
        }

        return result != 0 ? result : string.Compare(x.Product.Name, y.Product.Name, StringComparison.OrdinalIgnoreCase);
    });
}

public sealed record ReleaseNeighbours(Release? Older, Release? Newer)
{
    public static ReleaseNeighbours For(Release current, IEnumerable<Release> productReleases)
    {
        Release[] ordered = productReleases
            .Where(release => release.Product.Key == current.Product.Key)
            .Order(ReleaseOrdering.NewestFirst)
            .ToArray();

        var index = Array.FindIndex(ordered, release => release.Key == current.Key);
        if (index < 0)
        {
            return new ReleaseNeighbours(null, null);
        }

        return new ReleaseNeighbours(
            Older: index + 1 < ordered.Length ? ordered[index + 1] : null,
            Newer: index > 0 ? ordered[index - 1] : null);
    }
}
