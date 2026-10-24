using Microsoft.AspNetCore.Mvc;
using ReleaseNotes.Core.Catalog;
using Home = ReleaseNotes.Web.ContentModel.ContentTypes.Home;

namespace ReleaseNotes.Web.Rendering.Controllers;

public sealed class HomeController(SitePageDependencies dependencies) : SitePageController(dependencies)
{
    private const int RecentReleaseCount = 6;

    public override IActionResult Index()
    {
        CatalogSnapshot catalog = Catalog;
        var heroText = Text(Home.HeroText);

        var model = new HomePageModel(Meta(Page.Name, heroText, catalog.Links.HomeUrl), catalog.Links)
        {
            HeroTitle = Text(Home.HeroTitle) ?? Page.Name,
            HeroText = heroText,
            FeaturedProducts = catalog.Products
                .Where(product => product.IsFeatured)
                .Select(product => ProductCard.For(catalog, product))
                .ToArray(),
            RecentReleases = catalog.Releases
                .Take(RecentReleaseCount)
                .Select(release => ReleaseListItem.For(catalog, release))
                .ToArray(),
            ProductCount = catalog.Products.Count,
            ReleaseCount = catalog.Releases.Count,
            LastShipped = catalog.Releases.FirstOrDefault()?.ReleaseDate,
        };

        return PageView("Home", model);
    }
}
