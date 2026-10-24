using System.Globalization;
using ReleaseNotes.Core.Catalog;

namespace ReleaseNotes.Web.Rendering;

// The site is English only.
public static class Display
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

    public static string LongDate(DateOnly date) => date.ToString("MMMM d, yyyy", Culture);

    public static string ShortDate(DateOnly date) => date.ToString("MMM d, yyyy", Culture);

    public static string Month(DateOnly date) => date.ToString("MMMM yyyy", Culture);

    public static string IsoDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string Heading(this ChangeCategory category) => Words(category).Heading;

    public static string FilterLabel(this ChangeCategory category) => Words(category).Filter;

    // "1 feature", "3 fixes", "2 breaking changes"
    public static string Count(this ChangeCategory category, int count)
    {
        var words = Words(category);
        return Plural(count, words.Singular, words.Plural);
    }

    public static string Label(this ReleaseType type) => $"{type} release";

    public static string CssModifier(this ReleaseType type) => type.ToString().ToLowerInvariant();

    public static string Label(this ProductLifecycle lifecycle) =>
        lifecycle == ProductLifecycle.Maintenance ? "Maintenance mode" : lifecycle.ToString();

    public static string Plural(int count, string singular, string plural) =>
        string.Create(Culture, $"{count} {(count == 1 ? singular : plural)}");

    // Gives products without a logo a monogram colour that stays the same between requests.
    public static int Hue(string seed)
    {
        var hash = 0;
        foreach (var character in seed)
        {
            hash = unchecked((hash * 31) + character);
        }

        return Math.Abs(hash % 360);
    }

    private static (string Heading, string Filter, string Singular, string Plural) Words(ChangeCategory category) => category switch
    {
        ChangeCategory.Breaking => ("Breaking changes", "Breaking changes", "breaking change", "breaking changes"),
        ChangeCategory.Security => ("Security", "Security fixes", "security fix", "security fixes"),
        ChangeCategory.Feature => ("New features", "Features", "feature", "features"),
        ChangeCategory.Improvement => ("Improvements", "Improvements", "improvement", "improvements"),
        ChangeCategory.Fix => ("Fixes", "Fixes", "fix", "fixes"),
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };
}
