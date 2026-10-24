using System.Globalization;
using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Core.Versioning;

namespace ReleaseNotes.Core.Tests;

internal static class TestReleases
{
    public static readonly ProductReference ForgeKit = new(Guid.NewGuid(), "ForgeKit", "forgekit", "/products/forgekit/");
    public static readonly ProductReference Lumen = new(Guid.NewGuid(), "Lumen", "lumen", "/products/lumen/");

    public static Release Create(
        ProductReference product,
        string version,
        string date,
        bool markedAsBreaking = false,
        params ChangeCategory[] categories) =>
        new(
            Key: Guid.NewGuid(),
            Product: product,
            Version: SemanticVersion.Parse(version),
            ReleaseDate: DateOnly.Parse(date, CultureInfo.InvariantCulture),
            Type: ReleaseType.Minor,
            Summary: $"{product.Name} {version}",
            MarkedAsBreaking: markedAsBreaking,
            Url: $"{product.Url}{SemanticVersion.Parse(version).ToUrlSegment()}/",
            ExternalUrl: null,
            Notes: categories.Select((category, index) => new ReleaseNote($"Change {index}", category, null)).ToArray(),
            UpdatedAt: DateTime.UnixEpoch);
}
