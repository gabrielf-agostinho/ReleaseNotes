using ReleaseNotes.Core.Catalog;

namespace ReleaseNotes.Web.Api;

internal static class ChangelogApiMapping
{
    public static ProductReferenceDto ToDto(this ProductReference product, Func<string, string> absolute) =>
        new(product.Slug, product.Name, absolute(product.Url));

    public static ReleaseSummaryDto ToSummaryDto(this Release release, Func<string, string> absolute) =>
        new(
            release.Product.ToDto(absolute),
            release.Version.ToString(),
            release.Version.IsPreRelease,
            release.ReleaseDate,
            release.Type.ToString().ToLowerInvariant(),
            release.Summary,
            release.HasBreakingChanges,
            release.CategoryCounts.Select(c => new ChangeCountDto(c.Category.Slug(), c.Count)).ToArray(),
            absolute(release.Url),
            release.ExternalUrl);

    public static ReleaseDetailDto ToDetailDto(this Release release, Func<string, string> absolute) =>
        new(
            release.Product.ToDto(absolute),
            release.Version.ToString(),
            release.Version.IsPreRelease,
            release.ReleaseDate,
            release.Type.ToString().ToLowerInvariant(),
            release.Summary,
            release.HasBreakingChanges,
            release.Notes.Select(n => new ReleaseNoteDto(n.Title, n.Category.Slug(), n.DescriptionHtml)).ToArray(),
            absolute(release.Url),
            release.ExternalUrl);

    public static ProductSummaryDto ToSummaryDto(this Product product, CatalogSnapshot catalog, Func<string, string> absolute)
    {
        Release? latest = catalog.LatestReleaseOf(product.Key);
        return new ProductSummaryDto(
            product.Slug,
            product.Name,
            product.Tagline,
            product.Lifecycle.ToString().ToLowerInvariant(),
            absolute(product.Url),
            product.LogoUrl is null ? null : absolute(product.LogoUrl),
            product.WebsiteUrl,
            product.RepositoryUrl,
            latest?.Version.ToString(),
            latest?.ReleaseDate,
            catalog.ReleasesOf(product.Key).Count);
    }

    public static PagedResponse<TDto> ToResponse<T, TDto>(this PagedResult<T> page, Func<T, TDto> map) =>
        new(page.Items.Select(map).ToArray(), page.Page, page.PageSize, page.TotalItems, page.TotalPages);
}
