using Microsoft.AspNetCore.Http.Extensions;
using ReleaseNotes.Core.Catalog;

namespace ReleaseNotes.Web.Rendering;

// Title is without the site name; the layout appends it.
public sealed record PageMeta(
    string Title,
    string Description,
    string CanonicalPath,
    string OpenGraphType = "website",
    string? ImageUrl = null,
    bool NoIndex = false);

public abstract class SitePageModel
{
    protected SitePageModel(PageMeta meta, SiteLinks links)
    {
        Meta = meta;
        Links = links;
    }

    public PageMeta Meta { get; }

    public SiteLinks Links { get; }

    // Highlighted navigation entry: "products", "changelog" or none.
    public virtual string? Section => null;
}

public sealed record ReleaseListItem(Release Release, Product? Product, bool IsLatest)
{
    public static ReleaseListItem For(CatalogSnapshot catalog, Release release) =>
        new(release, catalog.FindProduct(release.Product.Key), catalog.IsLatest(release));
}

public sealed record ProductCard(Product Product, Release? LatestRelease, int ReleaseCount)
{
    public static ProductCard For(CatalogSnapshot catalog, Product product) =>
        new(product, catalog.LatestReleaseOf(product.Key), catalog.ReleasesOf(product.Key).Count);
}

public sealed record EmptyState(string Title, string Text, string? ActionUrl = null, string? ActionLabel = null);

public sealed class HomePageModel(PageMeta meta, SiteLinks links) : SitePageModel(meta, links)
{
    public required string HeroTitle { get; init; }

    public string? HeroText { get; init; }

    public required IReadOnlyList<ProductCard> FeaturedProducts { get; init; }

    public required IReadOnlyList<ReleaseListItem> RecentReleases { get; init; }

    public required int ProductCount { get; init; }

    public required int ReleaseCount { get; init; }

    public DateOnly? LastShipped { get; init; }
}

public sealed class ProductCatalogPageModel(PageMeta meta, SiteLinks links) : SitePageModel(meta, links)
{
    public override string Section => "products";

    public required string Title { get; init; }

    public string? Intro { get; init; }

    public required IReadOnlyList<ProductCard> Products { get; init; }
}

public sealed class ProductPageModel(PageMeta meta, SiteLinks links) : SitePageModel(meta, links)
{
    public override string Section => "products";

    public required Product Product { get; init; }

    public Release? LatestRelease { get; init; }

    public required PagedResult<ReleaseListItem> Releases { get; init; }

    public required Pagination Pagination { get; init; }
}

public sealed class ReleasePageModel(PageMeta meta, SiteLinks links) : SitePageModel(meta, links)
{
    public override string Section => "products";

    public required Release Release { get; init; }

    public required Product Product { get; init; }

    public required ReleaseNeighbours Neighbours { get; init; }

    public required bool IsLatest { get; init; }
}

public sealed class ChangelogPageModel(PageMeta meta, SiteLinks links) : SitePageModel(meta, links)
{
    public override string Section => "changelog";

    public required string Title { get; init; }

    public string? Intro { get; init; }

    public required string Path { get; init; }

    // Echoed back into the filter form as typed, including invalid values.
    public required ReleaseQueryParameters Query { get; init; }

    public required ReleaseFilter Filter { get; init; }

    public required IReadOnlyDictionary<string, string> Errors { get; init; }

    public required IReadOnlyList<Product> Products { get; init; }

    public required PagedResult<ReleaseListItem> Releases { get; init; }

    public required Pagination Pagination { get; init; }

    public IEnumerable<IGrouping<DateOnly, ReleaseListItem>> ReleasesByMonth =>
        Releases.Items.GroupBy(item => new DateOnly(item.Release.ReleaseDate.Year, item.Release.ReleaseDate.Month, 1));

    // 1-based positions of the first and last release on this page, for "Showing 11–20 of 42".
    public int FirstShown => Releases.TotalItems == 0 ? 0 : ((Releases.Page - 1) * Releases.PageSize) + 1;

    public int LastShown => Math.Min(Releases.Page * Releases.PageSize, Releases.TotalItems);

    public bool HasError(string parameter) => Errors.ContainsKey(parameter);

    // The current URL without one filter, for the "active filters" chips. Invalid values and the page are dropped.
    public string UrlWithout(string parameter)
    {
        var builder = new QueryBuilder();
        void Keep(string name, string? value)
        {
            if (name != parameter && !string.IsNullOrWhiteSpace(value) && !HasError(name))
            {
                builder.Add(name, value);
            }
        }

        Keep("product", Query.Product);
        Keep("category", Query.Category);
        Keep("from", Query.From);
        Keep("to", Query.To);
        Keep("breaking", Filter.BreakingOnly ? "true" : null);
        return Path + builder.ToQueryString();
    }
}

public sealed class NotFoundPageModel(PageMeta meta, SiteLinks links) : SitePageModel(meta, links)
{
    public static NotFoundPageModel Create(SiteLinks links) =>
        new(new PageMeta("Page not found", "The page you are looking for does not exist or is no longer published.", links.HomeUrl, NoIndex: true), links);
}

public sealed class ErrorPageModel(PageMeta meta, SiteLinks links) : SitePageModel(meta, links);
