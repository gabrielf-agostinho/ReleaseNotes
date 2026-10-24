using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ReleaseNotes.Web.Api;
using ReleaseNotes.Web.Tests.Infrastructure;

namespace ReleaseNotes.Web.Tests;

[Collection(SiteCollection.Name)]
public sealed class ChangelogApiTests(ReleaseNotesSite site)
{
    // 14 published demo releases; the ForgeKit 1.5.0-beta.1 draft must never appear.
    private const int PublishedDemoReleases = 14;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client = site.CreateSiteClient();

    private CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Releases_are_paged_newest_first_and_exclude_drafts()
    {
        PagedResponse<ReleaseSummaryDto>? first = await _client.GetFromJsonAsync<PagedResponse<ReleaseSummaryDto>>("/api/v1/releases?pageSize=5", Json, Cancellation);
        PagedResponse<ReleaseSummaryDto>? all = await _client.GetFromJsonAsync<PagedResponse<ReleaseSummaryDto>>("/api/v1/releases?pageSize=50", Json, Cancellation);

        Assert.NotNull(first);
        Assert.NotNull(all);
        Assert.Equal((1, 5, PublishedDemoReleases, 3), (first.Page, first.PageSize, first.TotalItems, first.TotalPages));
        Assert.Equal(5, first.Items.Count);
        Assert.Equal(all.Items.OrderByDescending(r => r.ReleaseDate).Select(r => r.ReleaseDate), all.Items.Select(r => r.ReleaseDate));
        Assert.DoesNotContain(all.Items, r => r.Version == "1.5.0-beta.1");
        Assert.StartsWith("http://localhost/products/", all.Items[0].Url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Filters_can_be_combined()
    {
        PagedResponse<ReleaseSummaryDto>? result = await _client.GetFromJsonAsync<PagedResponse<ReleaseSummaryDto>>(
            "/api/v1/releases?product=lumen&category=security&from=2026-01-01&to=2026-12-31", Json, Cancellation);

        Assert.NotNull(result);
        Assert.Equal(["2.1.0", "1.2.0"], result.Items.Select(r => r.Version));
        Assert.All(result.Items, r => Assert.Contains(r.Changes, c => c.Category == "security"));
    }

    [Fact]
    public async Task Breaking_filter_returns_only_incompatible_releases()
    {
        PagedResponse<ReleaseSummaryDto>? result = await _client.GetFromJsonAsync<PagedResponse<ReleaseSummaryDto>>("/api/v1/releases?breaking=true", Json, Cancellation);

        Assert.NotNull(result);
        ReleaseSummaryDto release = Assert.Single(result.Items);
        Assert.Equal(("lumen", "2.0.0", "major", true), (release.Product.Slug, release.Version, release.Type, release.HasBreakingChanges));
    }

    [Fact]
    public async Task Invalid_parameters_return_validation_problem_details()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/releases?product=nope&category=chore&page=0&pageSize=500&from=24-10-2026", Cancellation);
        using JsonDocument problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancellation));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            ["category", "from", "page", "pageSize", "product"],
            problem.RootElement.GetProperty("errors").EnumerateObject().Select(p => p.Name).Order());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Products_include_their_latest_stable_release()
    {
        ProductSummaryDto[]? products = await _client.GetFromJsonAsync<ProductSummaryDto[]>("/api/v1/products", Json, Cancellation);

        Assert.NotNull(products);
        Assert.Equal(["forgekit", "lumen", "relay"], products.Select(p => p.Slug));
        ProductSummaryDto forgeKit = products[0];
        Assert.Equal(("1.4.0", 6, "active"), (forgeKit.LatestVersion, forgeKit.ReleaseCount, forgeKit.Lifecycle));
        Assert.EndsWith(".svg", forgeKit.LogoUrl, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Latest_release_includes_notes_in_editorial_order()
    {
        ReleaseDetailDto? release = await _client.GetFromJsonAsync<ReleaseDetailDto>("/api/v1/products/forgekit/releases/latest", Json, Cancellation);

        Assert.NotNull(release);
        Assert.Equal("1.4.0", release.Version);
        Assert.Equal(["feature", "feature", "improvement", "improvement", "fix", "fix"], release.Notes.Select(n => n.Category));
        Assert.Equal("Plugin API", release.Notes[0].Title);
        Assert.Contains("<code>plugins</code>", release.Notes[0].DescriptionHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Latest_release_of_an_unknown_product_is_a_404_problem()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/products/unknown/releases/latest", Cancellation);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApi_document_describes_the_custom_endpoints()
    {
        using JsonDocument document = JsonDocument.Parse(await _client.GetStringAsync("/umbraco/swagger/release-notes/swagger.json", Cancellation));
        JsonElement paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/v1/releases", out _));
        Assert.True(paths.TryGetProperty("/api/v1/products", out _));
        Assert.True(paths.TryGetProperty("/api/v1/products/{slug}/releases/latest", out _));
    }
}
