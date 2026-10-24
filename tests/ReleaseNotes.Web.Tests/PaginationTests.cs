using ReleaseNotes.Web.Rendering;

namespace ReleaseNotes.Web.Tests;

public sealed class PaginationTests
{
    private static readonly KeyValuePair<string, string?>[] Query =
    [
        new("product", "lumen"),
        new("page", "3"),
        new("category", ""),
    ];

    [Fact]
    public void Links_keep_other_parameters_and_drop_empty_ones()
    {
        Pagination pagination = Pagination.Create("/changelog/", Query, page: 3, totalPages: 5);

        Assert.Equal("/changelog/?product=lumen&page=2", pagination.PreviousUrl);
        Assert.Equal("/changelog/?product=lumen&page=4", pagination.NextUrl);
        Assert.Equal("/changelog/?product=lumen", pagination.Pages[0]!.Url);
    }

    [Fact]
    public void Distant_pages_collapse_into_gaps()
    {
        Pagination pagination = Pagination.Create("/changelog/", [], page: 5, totalPages: 10);

        Assert.Equal([1, null, 4, 5, 6, null, 10], pagination.Pages.Select(p => p?.Number));
        Assert.True(pagination.Pages.Single(p => p?.Number == 5)!.IsCurrent);
    }

    [Fact]
    public void A_single_page_needs_no_pagination()
    {
        Pagination pagination = Pagination.Create("/changelog/", [], page: 1, totalPages: 1);

        Assert.False(pagination.IsVisible);
        Assert.Null(pagination.PreviousUrl);
        Assert.Null(pagination.NextUrl);
    }
}
