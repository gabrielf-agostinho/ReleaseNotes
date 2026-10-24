using System.Net;
using System.Xml.Linq;
using ReleaseNotes.Web.Tests.Infrastructure;

namespace ReleaseNotes.Web.Tests;

[Collection(SiteCollection.Name)]
public sealed class SeoTests(ReleaseNotesSite site)
{
    private readonly HttpClient _client = site.CreateSiteClient();

    [Fact]
    public async Task Sitemap_lists_published_pages_only()
    {
        HttpResponseMessage response = await _client.GetAsync("/sitemap.xml", TestContext.Current.CancellationToken);
        XDocument sitemap = XDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        string[] locations = sitemap.Descendants(ns + "loc").Select(e => e.Value).ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("http://localhost/products/forgekit/1-4-0/", locations);
        Assert.Contains("http://localhost/changelog/", locations);
        Assert.DoesNotContain(locations, l => l.Contains("1-5-0-beta-1", StringComparison.Ordinal));
        Assert.Equal(locations.Length, locations.Distinct().Count());
    }

    [Fact]
    public async Task Robots_points_to_the_sitemap_and_hides_the_backoffice()
    {
        var robots = await _client.GetStringAsync("/robots.txt", TestContext.Current.CancellationToken);

        Assert.Contains("Disallow: /umbraco/", robots, StringComparison.Ordinal);
        Assert.Contains("Sitemap: http://localhost/sitemap.xml", robots, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Release_pages_have_article_metadata_and_a_canonical_url()
    {
        var html = await _client.GetStringAsync("/products/forgekit/1-4-0/?utm_source=test", TestContext.Current.CancellationToken);

        Assert.Contains("<title>ForgeKit 1.4.0 release notes · Release Notes</title>", html, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"canonical\" href=\"http://localhost/products/forgekit/1-4-0/\" />", html, StringComparison.Ordinal);
        Assert.Contains("<meta property=\"og:type\" content=\"article\" />", html, StringComparison.Ordinal);
    }
}
