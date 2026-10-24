using ReleaseNotes.Core.Versioning;

namespace ReleaseNotes.Core.Tests;

public class SemanticVersionTests
{
    [Theory]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("  1.4.0 ", "1.4.0")]
    [InlineData("v2.0.0", "2.0.0")]
    [InlineData("V0.9.12", "0.9.12")]
    [InlineData("2.0.0-rc.1", "2.0.0-rc.1")]
    [InlineData("1.0.0-alpha+build.7", "1.0.0-alpha+build.7")]
    [InlineData("10.20.30", "10.20.30")]
    public void Parse_accepts_valid_versions_and_normalizes_them(string input, string expected)
    {
        Assert.True(SemanticVersion.TryParse(input, out SemanticVersion? version));
        Assert.Equal(expected, version.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1.4")]
    [InlineData("1.4.0.1")]
    [InlineData("01.4.0")]
    [InlineData("1.04.0")]
    [InlineData("1.4.0-")]
    [InlineData("1.4.0-beta..1")]
    [InlineData("1.4.0-01")]
    [InlineData("v")]
    [InlineData("vv1.0.0")]
    [InlineData("1.4.x")]
    [InlineData("99999999999.0.0")]
    public void Parse_rejects_invalid_versions(string? input)
    {
        Assert.False(SemanticVersion.TryParse(input, out _));
    }

    [Fact]
    public void Parse_throws_a_descriptive_format_exception()
    {
        FormatException exception = Assert.Throws<FormatException>(() => SemanticVersion.Parse("one.two"));
        Assert.Contains("one.two", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Precedence_follows_the_semver_specification_example()
    {
        // The ordered list from semver.org section 11.4.
        string[] ascending =
        [
            "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta",
            "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0",
        ];

        SemanticVersion[] shuffled = ascending.Reverse().Select(SemanticVersion.Parse).ToArray();

        Assert.Equal(ascending, shuffled.Order().Select(v => v.ToString()));
    }

    [Theory]
    [InlineData("1.10.0", "1.9.0")]
    [InlineData("2.0.0", "1.99.99")]
    [InlineData("1.0.1", "1.0.0")]
    [InlineData("1.0.0", "1.0.0-rc.1")]
    public void Numeric_parts_compare_numerically_not_alphabetically(string higher, string lower)
    {
        Assert.True(SemanticVersion.Parse(higher) > SemanticVersion.Parse(lower));
    }

    [Fact]
    public void Build_metadata_is_ignored_for_equality()
    {
        Assert.Equal(SemanticVersion.Parse("1.0.0+abc"), SemanticVersion.Parse("1.0.0+def"));
        Assert.Equal(SemanticVersion.Parse("1.0.0+abc").GetHashCode(), SemanticVersion.Parse("1.0.0").GetHashCode());
    }

    [Theory]
    [InlineData("1.4.0", "1-4-0")]
    [InlineData("2.0.0-RC.1", "2-0-0-rc-1")]
    [InlineData("1.0.0+build.5", "1-0-0")]
    public void Url_segment_has_no_dots(string input, string expected)
    {
        Assert.Equal(expected, SemanticVersion.Parse(input).ToUrlSegment());
    }
}
