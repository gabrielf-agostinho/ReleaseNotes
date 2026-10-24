using ReleaseNotes.Core.Catalog;
using static ReleaseNotes.Core.Tests.TestReleases;

namespace ReleaseNotes.Core.Tests;

public class CatalogSnapshotTests
{
    private static Product ProductFor(ProductReference reference) =>
        new(reference.Key, reference.Name, reference.Slug, reference.Url, "Tagline", null, null, null, null, ProductLifecycle.Active, false, DateTime.UnixEpoch);

    [Fact]
    public void Latest_release_prefers_the_newest_stable_version()
    {
        Release stable = Create(ForgeKit, "1.4.0", "2026-10-01");
        Release preRelease = Create(ForgeKit, "1.5.0-rc.1", "2026-10-20");
        var snapshot = new CatalogSnapshot([ProductFor(ForgeKit)], [stable, preRelease], new SiteLinks("/", null, null));

        Assert.Equal(stable, snapshot.LatestReleaseOf(ForgeKit.Key));
    }

    [Fact]
    public void Latest_release_falls_back_to_a_pre_release_when_nothing_is_stable()
    {
        Release beta = Create(Lumen, "0.1.0-beta.1", "2026-10-20");
        var snapshot = new CatalogSnapshot([ProductFor(Lumen)], [beta], new SiteLinks("/", null, null));

        Assert.Equal(beta, snapshot.LatestReleaseOf(Lumen.Key));
    }

    [Fact]
    public void Releases_are_exposed_newest_first_and_per_product()
    {
        Release a = Create(ForgeKit, "1.0.0", "2026-01-01");
        Release b = Create(Lumen, "1.0.0", "2026-02-01");
        Release c = Create(ForgeKit, "1.1.0", "2026-03-01");
        var snapshot = new CatalogSnapshot([ProductFor(Lumen), ProductFor(ForgeKit)], [a, b, c], new SiteLinks("/", null, null));

        Assert.Equal([c, b, a], snapshot.Releases);
        Assert.Equal([c, a], snapshot.ReleasesOf(ForgeKit.Key));
        Assert.Equal(["ForgeKit", "Lumen"], snapshot.Products.Select(p => p.Name));
        Assert.Empty(snapshot.ReleasesOf(Guid.NewGuid()));
        Assert.Null(snapshot.LatestReleaseOf(Guid.NewGuid()));
    }

    [Fact]
    public void Products_are_found_by_slug_case_insensitively()
    {
        var snapshot = new CatalogSnapshot([ProductFor(ForgeKit)], [], new SiteLinks("/", null, null));

        Assert.Equal(ForgeKit.Key, snapshot.FindProductBySlug("FORGEKIT")?.Key);
        Assert.Null(snapshot.FindProductBySlug("unknown"));
    }
}
