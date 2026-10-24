using ReleaseNotes.Core.Versioning;

namespace ReleaseNotes.Core.Catalog;

public enum ReleaseType
{
    Major,
    Minor,
    Patch,
}

public static class ReleaseTypes
{
    public static IReadOnlyList<string> Labels { get; } = Enum.GetNames<ReleaseType>();

    public static bool TryParseLabel(string? label, out ReleaseType type) =>
        Enum.TryParse(label?.Trim(), ignoreCase: true, out type) && Enum.IsDefined(type);

    // Null when there is no previous version or the version did not move forward.
    public static ReleaseType? Infer(SemanticVersion? previous, SemanticVersion current)
    {
        if (previous is null || current <= previous)
        {
            return null;
        }

        if (current.Major != previous.Major)
        {
            return ReleaseType.Major;
        }

        return current.Minor != previous.Minor ? ReleaseType.Minor : ReleaseType.Patch;
    }
}
