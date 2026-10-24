using ReleaseNotes.Core.Catalog;
using static ReleaseNotes.Core.Tests.TestReleases;

namespace ReleaseNotes.Core.Tests;

public class ReleaseOrderingTests
{
    [Fact]
    public void Newest_release_date_comes_first()
    {
        Release older = Create(ForgeKit, "1.0.0", "2026-01-10");
        Release newer = Create(ForgeKit, "1.1.0", "2026-03-02");
        Release newest = Create(Lumen, "0.2.0", "2026-05-20");

        Release[] ordered = new[] { older, newest, newer }.Order(ReleaseOrdering.NewestFirst).ToArray();

        Assert.Equal([newest, newer, older], ordered);
    }

    [Fact]
    public void Same_day_releases_are_ordered_by_version_precedence()
    {
        Release patch = Create(ForgeKit, "1.4.1", "2026-10-24");
        Release minor = Create(ForgeKit, "1.4.0", "2026-10-24");
        Release preRelease = Create(ForgeKit, "1.4.0-rc.1", "2026-10-24");

        Release[] ordered = new[] { preRelease, minor, patch }.Order(ReleaseOrdering.NewestFirst).ToArray();

        Assert.Equal([patch, minor, preRelease], ordered);
    }

    [Fact]
    public void Hotfix_of_an_older_line_shipped_later_is_listed_by_date()
    {
        Release major = Create(ForgeKit, "2.0.0", "2026-06-01");
        Release backport = Create(ForgeKit, "1.9.3", "2026-06-15");

        Release[] ordered = new[] { major, backport }.Order(ReleaseOrdering.NewestFirst).ToArray();

        Assert.Equal([backport, major], ordered);
    }

    [Fact]
    public void Neighbours_are_resolved_within_the_same_product_only()
    {
        Release v1 = Create(ForgeKit, "1.0.0", "2026-01-01");
        Release v2 = Create(ForgeKit, "1.1.0", "2026-02-01");
        Release v3 = Create(ForgeKit, "1.2.0", "2026-03-01");
        Release otherProduct = Create(Lumen, "5.0.0", "2026-01-15");

        ReleaseNeighbours middle = ReleaseNeighbours.For(v2, [v3, otherProduct, v1, v2]);
        ReleaseNeighbours first = ReleaseNeighbours.For(v1, [v3, v1, v2]);
        ReleaseNeighbours latest = ReleaseNeighbours.For(v3, [v3, v1, v2]);

        Assert.Equal((v1, v3), (middle.Older, middle.Newer));
        Assert.Equal((null, v2), (first.Older, first.Newer));
        Assert.Equal((v2, null), (latest.Older, latest.Newer));
    }
}
