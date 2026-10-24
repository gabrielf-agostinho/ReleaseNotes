using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Web.Rendering;

namespace ReleaseNotes.Web.Tests;

public sealed class ChangelogPageModelTests
{
    [Fact]
    public void Removing_a_filter_keeps_the_others()
    {
        ChangelogPageModel model = Create(new ReleaseQueryParameters(Product: "lumen", Category: "fix", Breaking: "true"));

        Assert.Equal("/changelog/?category=fix&breaking=true", model.UrlWithout("product"));
        Assert.Equal("/changelog/?product=lumen&category=fix", model.UrlWithout("breaking"));
    }

    [Fact]
    public void Removing_a_filter_drops_invalid_values()
    {
        var errors = new Dictionary<string, string> { ["from"] = "'from' must be a date in the format yyyy-MM-dd." };
        ChangelogPageModel model = Create(new ReleaseQueryParameters(Product: "lumen", From: "yesterday"), errors);

        Assert.Equal("/changelog/", model.UrlWithout("product"));
    }

    [Theory]
    [InlineData(1, 10, 25, 1, 10)]
    [InlineData(3, 10, 25, 21, 25)]
    [InlineData(1, 10, 0, 0, 0)]
    public void Shown_range_matches_the_current_page(int page, int pageSize, int totalItems, int first, int last)
    {
        var releases = new PagedResult<ReleaseListItem>([], page, pageSize, totalItems);
        ChangelogPageModel model = Create(new ReleaseQueryParameters(), releases: releases);

        Assert.Equal(first, model.FirstShown);
        Assert.Equal(last, model.LastShown);
    }

    private static ChangelogPageModel Create(
        ReleaseQueryParameters query,
        Dictionary<string, string>? errors = null,
        PagedResult<ReleaseListItem>? releases = null)
    {
        ReleaseFilterResult parsed = ReleaseFilterResult.Parse(query, ["lumen"]);
        var links = new SiteLinks("/", "/products/", "/changelog/");

        return new ChangelogPageModel(new PageMeta("Changelog", string.Empty, "/changelog/"), links)
        {
            Title = "Changelog",
            Path = "/changelog/",
            Query = query,
            Filter = parsed.Filter,
            Errors = errors ?? new Dictionary<string, string>(parsed.Errors),
            Products = [],
            Releases = releases ?? new PagedResult<ReleaseListItem>([], 1, 10, 0),
            Pagination = Pagination.Create("/changelog/", [], 1, 0),
        };
    }
}
