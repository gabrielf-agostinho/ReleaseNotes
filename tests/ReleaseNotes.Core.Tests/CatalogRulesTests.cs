using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Core.Versioning;
using static ReleaseNotes.Core.Tests.TestReleases;

namespace ReleaseNotes.Core.Tests;

public class CatalogRulesTests
{
    [Fact]
    public void Notes_are_grouped_in_display_order_keeping_editor_order_inside_groups()
    {
        Release release = Create(ForgeKit, "2.0.0", "2026-10-24") with
        {
            Notes =
            [
                new ReleaseNote("Faster builds", ChangeCategory.Improvement, null),
                new ReleaseNote("Plugin API", ChangeCategory.Feature, null),
                new ReleaseNote("Drop .NET 8", ChangeCategory.Breaking, null),
                new ReleaseNote("Workspaces", ChangeCategory.Feature, null),
            ],
        };

        IReadOnlyList<ReleaseNoteGroup> groups = release.NoteGroups;

        Assert.Equal([ChangeCategory.Breaking, ChangeCategory.Feature, ChangeCategory.Improvement], groups.Select(g => g.Category));
        Assert.Equal(["Plugin API", "Workspaces"], groups[1].Notes.Select(n => n.Title));
        Assert.Equal([1, 2, 1], release.CategoryCounts.Select(c => c.Count));
    }

    [Fact]
    public void A_breaking_note_marks_the_release_as_breaking_even_without_the_flag()
    {
        Assert.True(Create(ForgeKit, "2.0.0", "2026-10-24", false, ChangeCategory.Breaking).HasBreakingChanges);
        Assert.True(Create(ForgeKit, "2.0.0", "2026-10-24", true, ChangeCategory.Fix).HasBreakingChanges);
        Assert.False(Create(ForgeKit, "1.0.1", "2026-10-24", false, ChangeCategory.Fix).HasBreakingChanges);
    }

    [Theory]
    [InlineData("Breaking Change", ChangeCategory.Breaking)]
    [InlineData(" security ", ChangeCategory.Security)]
    [InlineData("Fix", ChangeCategory.Fix)]
    public void Category_labels_round_trip(string label, ChangeCategory expected)
    {
        Assert.True(ChangeCategories.TryParseLabel(label, out ChangeCategory category));
        Assert.Equal(expected, category);
        Assert.True(ChangeCategories.TryParseSlug(category.Slug(), out ChangeCategory fromSlug));
        Assert.Equal(expected, fromSlug);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Chore")]
    public void Unknown_category_labels_are_rejected(string? label)
    {
        Assert.False(ChangeCategories.TryParseLabel(label, out _));
    }

    [Theory]
    [InlineData("1.4.0", "2.0.0", ReleaseType.Major)]
    [InlineData("1.4.0", "1.5.0", ReleaseType.Minor)]
    [InlineData("1.4.0", "1.4.1", ReleaseType.Patch)]
    [InlineData("1.4.0-rc.1", "1.4.0", ReleaseType.Patch)]
    public void Release_type_is_inferred_from_the_previous_version(string previous, string current, ReleaseType expected)
    {
        Assert.Equal(expected, ReleaseTypes.Infer(SemanticVersion.Parse(previous), SemanticVersion.Parse(current)));
    }

    [Theory]
    [InlineData(null, "1.0.0")]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("1.4.0", "1.3.9")]
    public void Release_type_cannot_be_inferred_without_forward_progress(string? previous, string current)
    {
        SemanticVersion? previousVersion = previous is null ? null : SemanticVersion.Parse(previous);
        Assert.Null(ReleaseTypes.Infer(previousVersion, SemanticVersion.Parse(current)));
    }

    [Theory]
    [InlineData("Deprecated", ProductLifecycle.Deprecated)]
    [InlineData("beta", ProductLifecycle.Beta)]
    [InlineData(null, ProductLifecycle.Active)]
    [InlineData("Retired", ProductLifecycle.Active)]
    public void Product_lifecycle_falls_back_to_active(string? label, ProductLifecycle expected)
    {
        Assert.Equal(expected, ProductLifecycles.ParseOrDefault(label));
    }
}
