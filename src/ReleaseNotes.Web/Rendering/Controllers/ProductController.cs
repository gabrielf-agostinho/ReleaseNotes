using Microsoft.AspNetCore.Mvc;
using ReleaseNotes.Core.Catalog;

namespace ReleaseNotes.Web.Rendering.Controllers;

public sealed class ProductController(SitePageDependencies dependencies) : SitePageController(dependencies)
{
    private const int ReleasesPerPage = 8;

    public override IActionResult Index()
    {
        CatalogSnapshot catalog = Catalog;
        Product? product = catalog.FindProduct(Page.Key);
        if (product is null)
        {
            return NotFoundPage();
        }

        var page = RequestedPage();
        PagedResult<ReleaseListItem> releases = PagedResult<Release>
            .Create(catalog.ReleasesOf(product.Key), page, ReleasesPerPage)
            .Map(release => ReleaseListItem.For(catalog, release));

        var model = new ProductPageModel(
            Meta($"{product.Name} release notes", product.Tagline, WithPage(product.Url, page), imageUrl: product.LogoUrl),
            catalog.Links)
        {
            Product = product,
            LatestRelease = catalog.LatestReleaseOf(product.Key),
            Releases = releases,
            Pagination = Pagination.Create(product.Url, [], page, releases.TotalPages),
        };

        var outOfRange = releases.TotalItems > 0 && page > releases.TotalPages;
        return PageView("Product", model, outOfRange ? StatusCodes.Status404NotFound : StatusCodes.Status200OK);
    }
}
