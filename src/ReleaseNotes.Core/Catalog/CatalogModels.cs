using ReleaseNotes.Core.Versioning;

namespace ReleaseNotes.Core.Catalog;

public sealed record Product(
    Guid Key,
    string Name,
    string Slug,
    string Url,
    string Tagline,
    string? DescriptionHtml,
    string? LogoUrl,
    string? WebsiteUrl,
    string? RepositoryUrl,
    ProductLifecycle Lifecycle,
    bool IsFeatured,
    DateTime UpdatedAt)
{
    public ProductReference ToReference() => new(Key, Name, Slug, Url);
}

public sealed record ProductReference(Guid Key, string Name, string Slug, string Url);

public sealed record ReleaseNote(string Title, ChangeCategory Category, string? DescriptionHtml);

public sealed record Release(
    Guid Key,
    ProductReference Product,
    SemanticVersion Version,
    DateOnly ReleaseDate,
    ReleaseType Type,
    string Summary,
    bool MarkedAsBreaking,
    string Url,
    string? ExternalUrl,
    IReadOnlyList<ReleaseNote> Notes,
    DateTime UpdatedAt)
{
    // Either signal is enough: readers must never miss an incompatible change.
    public bool HasBreakingChanges =>
        MarkedAsBreaking || HasCategory(ChangeCategory.Breaking);

    public bool HasCategory(ChangeCategory category) => Notes.Any(note => note.Category == category);

    public IReadOnlyList<CategoryCount> CategoryCounts =>
        NoteGroups.Select(group => new CategoryCount(group.Category, group.Notes.Count)).ToArray();

    // Inside each group the editor's block order is kept: it is the editorial display order.
    public IReadOnlyList<ReleaseNoteGroup> NoteGroups =>
        ChangeCategories.All
            .Select(category => new ReleaseNoteGroup(category, Notes.Where(note => note.Category == category).ToArray()))
            .Where(group => group.Notes.Count > 0)
            .ToArray();
}

public sealed record CategoryCount(ChangeCategory Category, int Count);

public sealed record ReleaseNoteGroup(ChangeCategory Category, IReadOnlyList<ReleaseNote> Notes);
