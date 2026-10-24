using System.Globalization;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Web.Catalog;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;

namespace ReleaseNotes.Web.Rendering.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SeoController : Controller
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private readonly PublishedCatalogReader _catalogReader;
    private readonly IUmbracoContextFactory _umbracoContextFactory;

    public SeoController(PublishedCatalogReader catalogReader, IUmbracoContextFactory umbracoContextFactory)
    {
        _catalogReader = catalogReader;
        _umbracoContextFactory = umbracoContextFactory;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Sitemap()
    {
        // Umbraco skips creating a context for URLs with a file extension, and GetUrl needs one.
        using UmbracoContextReference _ = _umbracoContextFactory.EnsureUmbracoContext();
        CatalogSnapshot catalog = _catalogReader.GetSnapshot();
        DateTime? newest = catalog.Releases.Count > 0 ? catalog.Releases.Max(r => r.UpdatedAt) : null;

        IEnumerable<(string Url, DateTime? LastModified)> entries =
        [
            (catalog.Links.HomeUrl, newest),
            .. new[] { catalog.Links.ProductsUrl, catalog.Links.ChangelogUrl }.OfType<string>().Select(url => (url, newest)),
            .. catalog.Products.Select(p => (p.Url, (DateTime?)p.UpdatedAt)),
            .. catalog.Releases.Select(r => (r.Url, (DateTime?)r.UpdatedAt)),
        ];

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(
                SitemapNamespace + "urlset",
                entries.DistinctBy(entry => entry.Url).Select(entry => new XElement(
                    SitemapNamespace + "url",
                    new XElement(SitemapNamespace + "loc", Absolute(entry.Url)),
                    entry.LastModified is { } lastModified
                        ? new XElement(SitemapNamespace + "lastmod", lastModified.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                        : null))));

        return Content(document.Declaration + Environment.NewLine + document.Root, "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    [ResponseCache(Duration = 86400)]
    public IActionResult Robots() =>
        Content(
            $"""
            User-agent: *
            Allow: /
            Disallow: /umbraco/

            Sitemap: {Absolute("/sitemap.xml")}
            """,
            "text/plain; charset=utf-8");

    private string Absolute(string url) => Request.ToAbsolute(url);
}
