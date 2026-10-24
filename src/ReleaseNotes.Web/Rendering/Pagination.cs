using System.Globalization;
using Microsoft.AspNetCore.Http.Extensions;

namespace ReleaseNotes.Web.Rendering;

public sealed record PageLink(int Number, string Url, bool IsCurrent);

// First page, last page and a window around the current one. Other query parameters are kept.
public sealed class Pagination
{
    private const int Window = 1;

    private Pagination(IReadOnlyList<PageLink?> pages, string? previousUrl, string? nextUrl)
    {
        Pages = pages;
        PreviousUrl = previousUrl;
        NextUrl = nextUrl;
    }

    // A null entry is a gap ("…").
    public IReadOnlyList<PageLink?> Pages { get; }

    public string? PreviousUrl { get; }

    public string? NextUrl { get; }

    public bool IsVisible => Pages.Count > 1;

    public static Pagination Create(string path, IEnumerable<KeyValuePair<string, string?>> query, int page, int totalPages)
    {
        KeyValuePair<string, string?>[] kept = query
            .Where(pair => !string.Equals(pair.Key, "page", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pair.Value))
            .ToArray();

        string UrlFor(int number)
        {
            var builder = new QueryBuilder(kept.Select(pair => new KeyValuePair<string, string>(pair.Key, pair.Value!)));
            if (number > 1)
            {
                builder.Add("page", number.ToString(CultureInfo.InvariantCulture));
            }

            return path + builder.ToQueryString();
        }

        var pages = new List<PageLink?>();
        for (var number = 1; number <= totalPages; number++)
        {
            var inWindow = Math.Abs(number - page) <= Window;
            if (number == 1 || number == totalPages || inWindow)
            {
                pages.Add(new PageLink(number, UrlFor(number), number == page));
            }
            else if (pages.Count > 0 && pages[^1] is not null)
            {
                pages.Add(null);
            }
        }

        return new Pagination(
            pages,
            page > 1 && page <= totalPages ? UrlFor(page - 1) : null,
            page < totalPages ? UrlFor(page + 1) : null);
    }
}
