using ReleaseNotes.Core.Catalog;
using static ReleaseNotes.Core.Tests.TestReleases;

namespace ReleaseNotes.Core.Tests;

public class ReleaseFilterTests
{
    private static readonly string[] KnownProducts = ["forgekit", "lumen"];

    private static readonly Release[] Releases =
    [
        Create(ForgeKit, "1.0.0", "2026-01-10", false, ChangeCategory.Feature),
        Create(ForgeKit, "1.1.0", "2026-02-10", false, ChangeCategory.Feature, ChangeCategory.Fix),
        Create(ForgeKit, "2.0.0", "2026-04-01", false, ChangeCategory.Breaking, ChangeCategory.Feature),
        Create(Lumen, "0.9.0", "2026-03-05", false, ChangeCategory.Security),
        Create(Lumen, "1.0.0", "2026-05-05", true, ChangeCategory.Improvement),
    ];

    [Fact]
    public void Empty_parameters_produce_a_valid_unfiltered_first_page()
    {
        ReleaseFilterResult result = ReleaseFilterResult.Parse(new ReleaseQueryParameters(), KnownProducts);

        Assert.True(result.IsValid);
        Assert.False(result.Filter.HasCriteria);
        Assert.Equal(1, result.Filter.Page);
        Assert.Equal(ReleaseFilter.DefaultPageSize, result.Filter.PageSize);
    }

    [Fact]
    public void Valid_parameters_are_parsed_case_insensitively()
    {
        ReleaseFilterResult result = ReleaseFilterResult.Parse(
            new ReleaseQueryParameters("ForgeKit", "FIX", "2026-01-01", "2026-12-31", "true", "2", "5"),
            KnownProducts);

        Assert.True(result.IsValid);
        Assert.Equal(
            new ReleaseFilter
            {
                ProductSlug = "forgekit",
                Category = ChangeCategory.Fix,
                From = new DateOnly(2026, 1, 1),
                To = new DateOnly(2026, 12, 31),
                BreakingOnly = true,
                Page = 2,
                PageSize = 5,
            },
            result.Filter);
    }

    [Theory]
    [InlineData("product", "unknown", null, null, null, null, null)]
    [InlineData("category", null, "chore", null, null, null, null)]
    [InlineData("from", null, null, "24/10/2026", null, null, null)]
    [InlineData("breaking", null, null, null, "yes", null, null)]
    [InlineData("page", null, null, null, null, "0", null)]
    [InlineData("page", null, null, null, null, "-1", null)]
    [InlineData("page", null, null, null, null, "abc", null)]
    [InlineData("pageSize", null, null, null, null, null, "51")]
    [InlineData("pageSize", null, null, null, null, null, "0")]
    public void Invalid_values_are_reported_by_parameter_name(
        string expectedKey, string? product, string? category, string? from, string? breaking, string? page, string? pageSize)
    {
        ReleaseFilterResult result = ReleaseFilterResult.Parse(
            new ReleaseQueryParameters(product, category, from, null, breaking, page, pageSize),
            KnownProducts);

        Assert.False(result.IsValid);
        Assert.Equal([expectedKey], result.Errors.Keys);
    }

    [Fact]
    public void A_reversed_period_is_rejected_and_the_valid_part_is_kept()
    {
        ReleaseFilterResult result = ReleaseFilterResult.Parse(
            new ReleaseQueryParameters(From: "2026-06-01", To: "2026-01-01"),
            KnownProducts);

        Assert.Contains("to", result.Errors.Keys);
        Assert.Equal(new DateOnly(2026, 6, 1), result.Filter.From);
        Assert.Null(result.Filter.To);
    }

    [Fact]
    public void Filters_by_product()
    {
        PagedResult<Release> page = new ReleaseFilter { ProductSlug = "lumen" }.Apply(Releases);

        Assert.All(page.Items, release => Assert.Equal("lumen", release.Product.Slug));
        Assert.Equal(2, page.TotalItems);
    }

    [Fact]
    public void Filters_by_note_category()
    {
        PagedResult<Release> page = new ReleaseFilter { Category = ChangeCategory.Feature }.Apply(Releases);

        Assert.Equal(["2.0.0", "1.1.0", "1.0.0"], page.Items.Select(r => r.Version.ToString()));
    }

    [Fact]
    public void Breaking_category_includes_releases_flagged_by_the_editor()
    {
        PagedResult<Release> byCategory = new ReleaseFilter { Category = ChangeCategory.Breaking }.Apply(Releases);
        PagedResult<Release> byFlag = new ReleaseFilter { BreakingOnly = true }.Apply(Releases);

        Assert.Equal(["Lumen 1.0.0", "ForgeKit 2.0.0"], byCategory.Items.Select(r => r.Summary));
        Assert.Equal(byCategory.Items, byFlag.Items);
    }

    [Fact]
    public void Period_bounds_are_inclusive()
    {
        PagedResult<Release> page = new ReleaseFilter
        {
            From = new DateOnly(2026, 2, 10),
            To = new DateOnly(2026, 4, 1),
        }.Apply(Releases);

        Assert.Equal(["2.0.0", "0.9.0", "1.1.0"], page.Items.Select(r => r.Version.ToString()));
    }

    [Fact]
    public void Results_are_ordered_newest_first_and_paged()
    {
        PagedResult<Release> first = new ReleaseFilter { PageSize = 2 }.Apply(Releases);
        PagedResult<Release> last = new ReleaseFilter { PageSize = 2, Page = 3 }.Apply(Releases);

        Assert.Equal(["2026-05-05", "2026-04-01"], first.Items.Select(r => r.ReleaseDate.ToString("yyyy-MM-dd", null)));
        Assert.Equal((5, 3, false, true), (first.TotalItems, first.TotalPages, first.HasPrevious, first.HasNext));
        Assert.Equal(["2026-01-10"], last.Items.Select(r => r.ReleaseDate.ToString("yyyy-MM-dd", null)));
        Assert.Equal((true, false), (last.HasPrevious, last.HasNext));
    }

    [Fact]
    public void A_page_beyond_the_end_is_empty_but_keeps_the_totals()
    {
        PagedResult<Release> page = new ReleaseFilter { PageSize = 2, Page = 9 }.Apply(Releases);

        Assert.Empty(page.Items);
        Assert.Equal((5, 3, false), (page.TotalItems, page.TotalPages, page.HasNext));
    }

    [Fact]
    public void No_match_produces_an_empty_result_without_pages()
    {
        PagedResult<Release> page = new ReleaseFilter { From = new DateOnly(2030, 1, 1) }.Apply(Releases);

        Assert.Empty(page.Items);
        Assert.Equal((0, 0, false, false), (page.TotalItems, page.TotalPages, page.HasPrevious, page.HasNext));
    }
}
