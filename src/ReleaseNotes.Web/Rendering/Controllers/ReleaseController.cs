using Microsoft.AspNetCore.Mvc;
using ReleaseNotes.Core.Catalog;

namespace ReleaseNotes.Web.Rendering.Controllers;

public sealed class ReleaseController(SitePageDependencies dependencies) : SitePageController(dependencies)
{
    public override IActionResult Index()
    {
        CatalogSnapshot catalog = Catalog;
        Release? release = catalog.FindRelease(Page.Key);
        Product? product = release is null ? null : catalog.FindProduct(release.Product.Key);
        if (release is null || product is null)
        {
            // Published but skipped by the catalog reader, e.g. an unparseable version.
            return NotFoundPage();
        }

        var model = new ReleasePageModel(
            Meta($"{product.Name} {release.Version} release notes", release.Summary, release.Url, "article", product.LogoUrl),
            catalog.Links)
        {
            Release = release,
            Product = product,
            Neighbours = catalog.NeighboursOf(release),
            IsLatest = catalog.IsLatest(release),
        };

        return PageView("Release", model);
    }
}
