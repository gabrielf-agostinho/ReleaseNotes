using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ReleaseNotes.Core.Versioning;

// Semantic Versioning 2.0.0 (https://semver.org). Strict about the format, but surrounding whitespace
// and a leading "v" are accepted, because that is what editors type.
public sealed partial class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    // The regex from semver.org, with numbered groups.
    [GeneratedRegex(
        @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)" +
        @"(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?" +
        @"(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex SemVerPattern();

    private readonly string[] _preReleaseIdentifiers;

    private SemanticVersion(int major, int minor, int patch, string? preRelease, string? buildMetadata)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
        BuildMetadata = buildMetadata;
        _preReleaseIdentifiers = preRelease?.Split('.') ?? [];
    }

    public int Major { get; }

    public int Minor { get; }

    public int Patch { get; }

    // "beta.2", without the hyphen.
    public string? PreRelease { get; }

    // Without the plus sign. Ignored for precedence and equality.
    public string? BuildMetadata { get; }

    public bool IsPreRelease => PreRelease is not null;

    public static SemanticVersion Parse(string value) =>
        TryParse(value, out SemanticVersion? version)
            ? version
            : throw new FormatException($"'{value}' is not a valid semantic version (expected MAJOR.MINOR.PATCH, e.g. 1.4.0).");

    public static bool TryParse([NotNullWhen(true)] string? value, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> candidate = value.AsSpan().Trim();
        if (candidate.Length > 1 && (candidate[0] == 'v' || candidate[0] == 'V'))
        {
            candidate = candidate[1..];
        }

        Match match = SemVerPattern().Match(candidate.ToString());
        if (!match.Success
            || !TryParseNumber(match.Groups[1].Value, out var major)
            || !TryParseNumber(match.Groups[2].Value, out var minor)
            || !TryParseNumber(match.Groups[3].Value, out var patch))
        {
            return false;
        }

        version = new SemanticVersion(
            major,
            minor,
            patch,
            match.Groups[4].Success ? match.Groups[4].Value : null,
            match.Groups[5].Success ? match.Groups[5].Value : null);
        return true;
    }

    // "2.0.0-rc.1" -> "2-0-0-rc-1". A dotted last segment would be treated as a static file request.
    public string ToUrlSegment() =>
        ToString(includeBuildMetadata: false).Replace('.', '-').ToLowerInvariant();

    public override string ToString() => ToString(includeBuildMetadata: true);

    public string ToString(bool includeBuildMetadata)
    {
        var core = string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}");
        if (PreRelease is not null)
        {
            core += "-" + PreRelease;
        }

        if (includeBuildMetadata && BuildMetadata is not null)
        {
            core += "+" + BuildMetadata;
        }

        return core;
    }

    // Precedence rules from section 11 of the spec.
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var result = (Major, Minor, Patch).CompareTo((other.Major, other.Minor, other.Patch));
        if (result != 0)
        {
            return result;
        }

        // 1.0.0-rc.1 < 1.0.0
        if (!IsPreRelease || !other.IsPreRelease)
        {
            return other.IsPreRelease.CompareTo(IsPreRelease);
        }

        var length = Math.Min(_preReleaseIdentifiers.Length, other._preReleaseIdentifiers.Length);
        for (var i = 0; i < length; i++)
        {
            result = ComparePreReleaseIdentifier(_preReleaseIdentifiers[i], other._preReleaseIdentifiers[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return _preReleaseIdentifiers.Length.CompareTo(other._preReleaseIdentifiers.Length);
    }

    public bool Equals(SemanticVersion? other) => other is not null && CompareTo(other) == 0;

    public override bool Equals(object? obj) => obj is SemanticVersion other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, PreRelease);

    public static bool operator ==(SemanticVersion? left, SemanticVersion? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(SemanticVersion? left, SemanticVersion? right) => !(left == right);

    public static bool operator <(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(SemanticVersion left, SemanticVersion right) => left.CompareTo(right) >= 0;

    private static int ComparePreReleaseIdentifier(string left, string right)
    {
        var leftIsNumeric = int.TryParse(left, NumberStyles.None, CultureInfo.InvariantCulture, out var leftNumber);
        var rightIsNumeric = int.TryParse(right, NumberStyles.None, CultureInfo.InvariantCulture, out var rightNumber);

        return (leftIsNumeric, rightIsNumeric) switch
        {
            (true, true) => leftNumber.CompareTo(rightNumber),
            (true, false) => -1,
            (false, true) => 1,
            _ => string.CompareOrdinal(left, right),
        };
    }

    private static bool TryParseNumber(string value, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
}
