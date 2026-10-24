using Microsoft.AspNetCore.Mvc;
using ReleaseNotes.Core.Catalog;
using Changelog = ReleaseNotes.Web.ContentModel.ContentTypes.Changelog;

namespace ReleaseNotes.Web.Rendering.Controllers;

// Filters are a plain GET form: works without JavaScript and every filtered view has a shareable URL.
public sealed class ChangelogController(SitePageDependencies dependencies) : SitePageController(dependencies)
{
    public override IActionResult Index()
    {
        CatalogSnapshot catalog = Catalog;
        IQueryCollection query = Request.Query;
        var parameters = new ReleaseQueryParameters(
            Product: query["product"],
            Category: query["category"],
            From: query["from"],
            To: query["to"],
            Breaking: query["breaking"],
            Page: query["page"]);

        ReleaseFilterResult filterResult = ReleaseFilterResult.Parse(parameters, catalog.ProductSlugs);
        PagedResult<ReleaseListItem> releases = filterResult.Filter
            .Apply(catalog.Releases)
            .Map(release => ReleaseListItem.For(catalog, release));

        var path = catalog.Links.ChangelogUrl ?? Request.Path.Value ?? "/";
        var intro = Text(Changelog.Intro);

        // Filtered views get noindex: worth sharing, not worth indexing.
        var model = new ChangelogPageModel(
            Meta(Page.Name, intro, WithPage(path, filterResult.Filter.Page), noIndex: filterResult.Filter.HasCriteria),
            catalog.Links)
        {
            Title = Page.Name,
            Intro = intro,
            Path = path,
            Query = parameters,
            Filter = filterResult.Filter,
            Errors = filterResult.Errors,
            Products = catalog.Products,
            Releases = releases,
            Pagination = Pagination.Create(
                path,
                query.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value.ToString())),
                filterResult.Filter.Page,
                releases.TotalPages),
        };

        return PageView("Changelog", model, filterResult.IsValid ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest);
    }
}
