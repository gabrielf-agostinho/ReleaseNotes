using ReleaseNotes.Core.Catalog;

namespace ReleaseNotes.Core.Tests;

public class ReleaseVersionCheckTests
{
    [Fact]
    public void A_new_valid_version_passes_and_is_normalized()
    {
        ReleaseVersionCheck check = ReleaseVersionCheck.Run(" v1.5.0 ", "Minor", ["1.4.0", "1.3.1"]);

        Assert.True(check.IsValid);
        Assert.Equal("1.5.0", check.Version?.ToString());
        Assert.Null(check.Warning);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("latest")]
    public void An_invalid_version_is_rejected(string? version)
    {
        ReleaseVersionCheck check = ReleaseVersionCheck.Run(version, "Minor", []);

        Assert.False(check.IsValid);
        Assert.Contains("semantic version", check.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("1.4.0")]
    [InlineData("v1.4.0")]
    [InlineData("1.4.0+build.9")]
    public void A_version_that_already_exists_for_the_product_is_rejected(string version)
    {
        ReleaseVersionCheck check = ReleaseVersionCheck.Run(version, "Minor", ["1.4.0", "1.3.1"]);

        Assert.False(check.IsValid);
        Assert.Contains("already exists", check.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_sibling_versions_are_ignored()
    {
        ReleaseVersionCheck check = ReleaseVersionCheck.Run("1.0.0", "Major", ["not-a-version", null]);

        Assert.True(check.IsValid);
    }

    [Fact]
    public void A_release_type_that_does_not_match_the_bump_produces_a_warning()
    {
        ReleaseVersionCheck check = ReleaseVersionCheck.Run("2.0.0", "Minor", ["1.4.0"]);

        Assert.True(check.IsValid);
        Assert.Equal("Version 2.0.0 follows 1.4.0, which is a major bump, but the release type is Minor.", check.Warning);
    }

    [Fact]
    public void The_bump_is_measured_against_the_closest_lower_version()
    {
        // A backport between existing versions: 1.3.2 follows 1.3.1, not 2.0.0.
        ReleaseVersionCheck check = ReleaseVersionCheck.Run("1.3.2", "Patch", ["2.0.0", "1.3.1", "1.0.0"]);

        Assert.True(check.IsValid);
        Assert.Null(check.Warning);
    }
}
