using Microsoft.AspNetCore.Mvc;
using ReleaseNotes.Core.Catalog;
using ProductCatalog = ReleaseNotes.Web.ContentModel.ContentTypes.ProductCatalog;

namespace ReleaseNotes.Web.Rendering.Controllers;

public sealed class ProductCatalogController(SitePageDependencies dependencies) : SitePageController(dependencies)
{
    public override IActionResult Index()
    {
        CatalogSnapshot catalog = Catalog;
        var intro = Text(ProductCatalog.Intro);

        var model = new ProductCatalogPageModel(Meta(Page.Name, intro, catalog.Links.ProductsUrl ?? Request.Path), catalog.Links)
        {
            Title = Page.Name,
            Intro = intro,
            Products = catalog.Products.Select(product => ProductCard.For(catalog, product)).ToArray(),
        };

        return PageView("ProductCatalog", model);
    }
}
