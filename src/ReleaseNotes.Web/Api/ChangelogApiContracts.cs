namespace ReleaseNotes.Web.Api;

// The XML comments in this file and on the controller end up in the OpenAPI document.

/// <param name="Page">1-based.</param>
/// <param name="TotalPages">0 when nothing matches.</param>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);

public sealed record ProductReferenceDto(string Slug, string Name, string Url);

/// <param name="Category">feature, improvement, fix, security or breaking.</param>
public sealed record ChangeCountDto(string Category, int Count);

/// <summary>A release without its notes.</summary>
/// <param name="Version">Semantic version, e.g. 1.4.0.</param>
/// <param name="Type">major, minor or patch.</param>
/// <param name="HasBreakingChanges">True when flagged by the editor or when any note is a breaking change.</param>
/// <param name="Changes">Note counts per category, in display order.</param>
/// <param name="Url">Absolute URL of the release page.</param>
/// <param name="ExternalUrl">Link to an external release page or migration guide, if any.</param>
public sealed record ReleaseSummaryDto(
    ProductReferenceDto Product,
    string Version,
    bool IsPreRelease,
    DateOnly ReleaseDate,
    string Type,
    string Summary,
    bool HasBreakingChanges,
    IReadOnlyList<ChangeCountDto> Changes,
    string Url,
    string? ExternalUrl);

/// <param name="Category">feature, improvement, fix, security or breaking.</param>
/// <param name="DescriptionHtml">Sanitized HTML from the rich text editor, or null.</param>
public sealed record ReleaseNoteDto(string Title, string Category, string? DescriptionHtml);

/// <summary>A release with its notes in editorial order.</summary>
public sealed record ReleaseDetailDto(
    ProductReferenceDto Product,
    string Version,
    bool IsPreRelease,
    DateOnly ReleaseDate,
    string Type,
    string Summary,
    bool HasBreakingChanges,
    IReadOnlyList<ReleaseNoteDto> Notes,
    string Url,
    string? ExternalUrl);

/// <param name="Lifecycle">active, beta, maintenance or deprecated.</param>
/// <param name="LatestVersion">Newest stable version, or the newest pre-release if there is no stable one.</param>
public sealed record ProductSummaryDto(
    string Slug,
    string Name,
    string Tagline,
    string Lifecycle,
    string Url,
    string? LogoUrl,
    string? WebsiteUrl,
    string? RepositoryUrl,
    string? LatestVersion,
    DateOnly? LatestReleaseDate,
    int ReleaseCount);
