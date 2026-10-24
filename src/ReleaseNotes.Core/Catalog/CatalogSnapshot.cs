namespace ReleaseNotes.Core.Catalog;

// The products and changelog pages may be unpublished.
public sealed record SiteLinks(string HomeUrl, string? ProductsUrl, string? ChangelogUrl);

public sealed class CatalogSnapshot
{
    private readonly Dictionary<Guid, Release[]> _releasesByProduct;

    public CatalogSnapshot(IEnumerable<Product> products, IEnumerable<Release> releases, SiteLinks links)
    {
        Products = products.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        Releases = releases.Order(ReleaseOrdering.NewestFirst).ToArray();
        Links = links;
        _releasesByProduct = Releases
            .GroupBy(release => release.Product.Key)
            .ToDictionary(group => group.Key, group => group.ToArray());
    }

    public static CatalogSnapshot Empty { get; } = new([], [], new SiteLinks("/", null, null));

    // Ordered by name.
    public IReadOnlyList<Product> Products { get; }

    // Ordered newest first.
    public IReadOnlyList<Release> Releases { get; }

    public SiteLinks Links { get; }

    public IReadOnlyCollection<string> ProductSlugs => Products.Select(p => p.Slug).ToArray();

    public Product? FindProduct(Guid key) => Products.FirstOrDefault(p => p.Key == key);

    public Product? FindProductBySlug(string slug) =>
        Products.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

    public Release? FindRelease(Guid key) => Releases.FirstOrDefault(r => r.Key == key);

    public IReadOnlyList<Release> ReleasesOf(Guid productKey) =>
        _releasesByProduct.TryGetValue(productKey, out Release[]? releases) ? releases : [];

    // The newest stable release, or the newest pre-release when nothing stable has shipped yet.
    public Release? LatestReleaseOf(Guid productKey)
    {
        IReadOnlyList<Release> releases = ReleasesOf(productKey);
        return releases.FirstOrDefault(r => !r.Version.IsPreRelease) ?? releases.FirstOrDefault();
    }

    public bool IsLatest(Release release) => LatestReleaseOf(release.Product.Key)?.Key == release.Key;

    public ReleaseNeighbours NeighboursOf(Release release) => ReleaseNeighbours.For(release, ReleasesOf(release.Product.Key));
}
