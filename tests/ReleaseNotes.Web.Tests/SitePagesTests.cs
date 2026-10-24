using System.Net;
using ReleaseNotes.Web.Tests.Infrastructure;

namespace ReleaseNotes.Web.Tests;

[Collection(SiteCollection.Name)]
public sealed class SitePagesTests(ReleaseNotesSite site)
{
    private readonly HttpClient _client = site.CreateSiteClient();

    [Theory]
    [InlineData("/", "Every change, clearly told.")]
    [InlineData("/products/", "ForgeKit")]
    [InlineData("/products/forgekit/", "Release history")]
    [InlineData("/products/forgekit/1-4-0/", "Plugin API")]
    [InlineData("/changelog/", "Apply filters")]
    public async Task Public_pages_render_published_content(string path, string expectedText)
    {
        HttpResponseMessage response = await _client.GetAsync(path, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(expectedText, html, StringComparison.Ordinal);
        Assert.Contains("<link rel=\"canonical\"", html, StringComparison.Ordinal);
        Assert.Contains("<meta property=\"og:title\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Release_page_groups_notes_by_category_with_breaking_changes_first()
    {
        var html = await _client.GetStringAsync("/products/lumen/2-0-0/", TestContext.Current.CancellationToken);

        Assert.Contains("This release contains breaking changes", html, StringComparison.Ordinal);
        var breaking = html.IndexOf("id=\"breaking\"", StringComparison.Ordinal);
        var features = html.IndexOf("id=\"feature\"", StringComparison.Ordinal);
        var improvements = html.IndexOf("id=\"improvement\"", StringComparison.Ordinal);
        Assert.True(breaking > 0 && breaking < features && features < improvements, "Groups must follow the display order.");
    }

    [Fact]
    public async Task Release_page_links_to_its_neighbours_within_the_product()
    {
        var html = await _client.GetStringAsync("/products/lumen/2-0-0/", TestContext.Current.CancellationToken);

        Assert.Contains("href=\"/products/lumen/1-2-0/\" rel=\"prev\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/products/lumen/2-0-1/\" rel=\"next\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unpublished_draft_releases_are_not_routable()
    {
        HttpResponseMessage response = await _client.GetAsync("/products/forgekit/1-5-0-beta-1/", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_urls_render_the_site_404_page()
    {
        HttpResponseMessage response = await _client.GetAsync("/no/such/page/", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("This page is not in the changelog", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Changelog_filters_by_product_and_category()
    {
        var html = await _client.GetStringAsync("/changelog/?product=lumen&category=breaking", TestContext.Current.CancellationToken);

        Assert.Contains("/products/lumen/2-0-0/", html, StringComparison.Ordinal);
        Assert.DoesNotContain("/products/forgekit/", html, StringComparison.Ordinal);
        Assert.Contains("noindex", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Changelog_reports_invalid_filters_and_keeps_the_valid_ones()
    {
        HttpResponseMessage response = await _client.GetAsync("/changelog/?category=chore&product=relay", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("role=\"alert\"", html, StringComparison.Ordinal);
        Assert.Contains("Unknown category", html, StringComparison.Ordinal);
        Assert.Contains("/products/relay/0-2-0/", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Changelog_shows_an_empty_state_when_nothing_matches()
    {
        var html = await _client.GetStringAsync("/changelog/?from=2030-01-01", TestContext.Current.CancellationToken);

        Assert.Contains("No releases match these filters", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Product_history_page_past_the_end_is_not_found()
    {
        HttpResponseMessage response = await _client.GetAsync("/products/forgekit/?page=99", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
