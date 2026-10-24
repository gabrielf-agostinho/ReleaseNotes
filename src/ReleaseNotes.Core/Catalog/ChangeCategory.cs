using System.Diagnostics.CodeAnalysis;

namespace ReleaseNotes.Core.Catalog;

// Declaration order is the display order on a release page: what readers must act on comes first.
public enum ChangeCategory
{
    Breaking,
    Security,
    Feature,
    Improvement,
    Fix,
}

public static class ChangeCategories
{
    public static IReadOnlyList<ChangeCategory> All { get; } = Enum.GetValues<ChangeCategory>();

    // Stored by Umbraco as the dropdown values, so changing them breaks existing content.
    public static IReadOnlyList<string> Labels { get; } = All.Select(Label).ToArray();

    public static string Label(this ChangeCategory category) => category switch
    {
        ChangeCategory.Breaking => "Breaking Change",
        _ => category.ToString(),
    };

    public static string Slug(this ChangeCategory category) => category.ToString().ToLowerInvariant();

    public static bool TryParseLabel([NotNullWhen(true)] string? label, out ChangeCategory category) =>
        TryParse(label, Label, out category);

    public static bool TryParseSlug([NotNullWhen(true)] string? slug, out ChangeCategory category) =>
        TryParse(slug, Slug, out category);

    private static bool TryParse(string? value, Func<ChangeCategory, string> format, out ChangeCategory category)
    {
        value = value?.Trim();
        foreach (ChangeCategory candidate in All)
        {
            if (string.Equals(format(candidate), value, StringComparison.OrdinalIgnoreCase))
            {
                category = candidate;
                return true;
            }
        }

        category = default;
        return false;
    }
}
