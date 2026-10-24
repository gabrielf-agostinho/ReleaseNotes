using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ReleaseNotes.Core.Catalog;

public sealed record ReleaseFilter
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public string? ProductSlug { get; init; }

    public ChangeCategory? Category { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public bool BreakingOnly { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public bool HasCriteria => ProductSlug is not null || Category is not null || From is not null || To is not null || BreakingOnly;

    public PagedResult<Release> Apply(IEnumerable<Release> releases)
    {
        IEnumerable<Release> query = releases;

        if (ProductSlug is not null)
        {
            query = query.Where(release => string.Equals(release.Product.Slug, ProductSlug, StringComparison.OrdinalIgnoreCase));
        }

        if (Category is { } category)
        {
            // A release flagged as breaking matches even if none of its notes is.
            query = category == ChangeCategory.Breaking
                ? query.Where(release => release.HasBreakingChanges)
                : query.Where(release => release.HasCategory(category));
        }

        if (From is { } from)
        {
            query = query.Where(release => release.ReleaseDate >= from);
        }

        if (To is { } to)
        {
            query = query.Where(release => release.ReleaseDate <= to);
        }

        if (BreakingOnly)
        {
            query = query.Where(release => release.HasBreakingChanges);
        }

        return PagedResult<Release>.Create(query.Order(ReleaseOrdering.NewestFirst), Page, PageSize);
    }
}

// Raw query string values; the changelog page and the API share the same parsing.
public sealed record ReleaseQueryParameters(
    string? Product = null,
    string? Category = null,
    string? From = null,
    string? To = null,
    string? Breaking = null,
    string? Page = null,
    string? PageSize = null);

// Filter keeps the valid subset of the parameters, so the page can still list releases next to the errors.
public sealed record ReleaseFilterResult(ReleaseFilter Filter, IReadOnlyDictionary<string, string> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static ReleaseFilterResult Parse(
        ReleaseQueryParameters parameters,
        IReadOnlyCollection<string> knownProductSlugs,
        int defaultPageSize = ReleaseFilter.DefaultPageSize)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var filter = new ReleaseFilter { PageSize = defaultPageSize };

        if (HasValue(parameters.Product))
        {
            var slug = parameters.Product.Trim();
            if (knownProductSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase))
            {
                filter = filter with { ProductSlug = slug.ToLowerInvariant() };
            }
            else
            {
                errors["product"] = $"Unknown product '{slug}'.";
            }
        }

        if (HasValue(parameters.Category))
        {
            if (ChangeCategories.TryParseSlug(parameters.Category, out ChangeCategory category))
            {
                filter = filter with { Category = category };
            }
            else
            {
                var allowed = string.Join(", ", ChangeCategories.All.Select(c => c.Slug()));
                errors["category"] = $"Unknown category '{parameters.Category.Trim()}'. Allowed values: {allowed}.";
            }
        }

        DateOnly? from = ParseDate(parameters.From, "from", errors);
        DateOnly? to = ParseDate(parameters.To, "to", errors);
        if (from is not null && to is not null && from > to)
        {
            errors["to"] = "'to' must be on or after 'from'.";
            to = null;
        }

        filter = filter with { From = from, To = to };

        if (HasValue(parameters.Breaking))
        {
            if (bool.TryParse(parameters.Breaking.Trim(), out var breakingOnly))
            {
                filter = filter with { BreakingOnly = breakingOnly };
            }
            else
            {
                errors["breaking"] = "'breaking' must be 'true' or 'false'.";
            }
        }

        if (HasValue(parameters.Page))
        {
            if (TryParsePositive(parameters.Page, out var page))
            {
                filter = filter with { Page = page };
            }
            else
            {
                errors["page"] = "'page' must be a whole number greater than or equal to 1.";
            }
        }

        if (HasValue(parameters.PageSize))
        {
            if (TryParsePositive(parameters.PageSize, out var pageSize) && pageSize <= ReleaseFilter.MaxPageSize)
            {
                filter = filter with { PageSize = pageSize };
            }
            else
            {
                errors["pageSize"] = $"'pageSize' must be a whole number between 1 and {ReleaseFilter.MaxPageSize}.";
            }
        }

        return new ReleaseFilterResult(filter, errors);
    }

    private static bool HasValue([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value);

    private static DateOnly? ParseDate(string? value, string name, Dictionary<string, string> errors)
    {
        if (!HasValue(value))
        {
            return null;
        }

        if (DateOnly.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            return date;
        }

        errors[name] = $"'{name}' must be a date in the format yyyy-MM-dd.";
        return null;
    }

    private static bool TryParsePositive(string value, out int number) =>
        int.TryParse(value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out number) && number >= 1;
}
