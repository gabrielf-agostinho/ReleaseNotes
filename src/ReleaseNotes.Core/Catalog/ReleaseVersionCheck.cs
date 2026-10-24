using ReleaseNotes.Core.Versioning;

namespace ReleaseNotes.Core.Catalog;

// Run when a release is saved. An invalid or duplicate version is an error; a release type that does
// not match the version bump is only a warning, since editors sometimes know better.
public sealed record ReleaseVersionCheck(SemanticVersion? Version, string? Error, string? Warning)
{
    public bool IsValid => Error is null;

    public static ReleaseVersionCheck Run(string? rawVersion, string? releaseTypeLabel, IEnumerable<string?> otherVersions)
    {
        if (!SemanticVersion.TryParse(rawVersion, out SemanticVersion? version))
        {
            return new ReleaseVersionCheck(
                null,
                $"'{rawVersion}' is not a valid semantic version. Use MAJOR.MINOR.PATCH, for example 1.4.0.",
                null);
        }

        SemanticVersion[] others = otherVersions
            .Select(raw => SemanticVersion.TryParse(raw, out SemanticVersion? parsed) ? parsed : null)
            .OfType<SemanticVersion>()
            .ToArray();

        if (others.Contains(version))
        {
            return new ReleaseVersionCheck(version, $"Version {version} already exists for this product.", null);
        }

        SemanticVersion? previous = others.Where(other => other < version).DefaultIfEmpty().Max();
        ReleaseType? expected = ReleaseTypes.Infer(previous, version);

        string? warning = expected is { } inferred
                          && ReleaseTypes.TryParseLabel(releaseTypeLabel, out ReleaseType selected)
                          && selected != inferred
            ? $"Version {version} follows {previous}, which is a {inferred.ToString().ToLowerInvariant()} bump, but the release type is {selected}."
            : null;

        return new ReleaseVersionCheck(version, null, warning);
    }
}
