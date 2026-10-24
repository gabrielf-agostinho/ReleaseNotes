using ReleaseNotes.Core.Versioning;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

namespace ReleaseNotes.Web.ContentModel;

// Release URLs come from the version, not the node name, so a node named "1.4.0 – Workspaces" still
// lives at /products/forgekit/1-4-0/. Returning null falls back to Umbraco's default provider.
public sealed class ReleaseUrlSegmentProvider : IUrlSegmentProvider
{
    public string? GetUrlSegment(IContentBase content, string? culture = null) =>
        GetUrlSegment(content, published: false, culture);

    public string? GetUrlSegment(IContentBase content, bool published, string? culture = null)
    {
        if (content.ContentType.Alias != ContentTypes.Release.Alias)
        {
            return null;
        }

        var version = content.GetValue<string>(ContentTypes.Release.Version, culture, segment: null, published);
        return SemanticVersion.TryParse(version, out SemanticVersion? parsed) ? parsed.ToUrlSegment() : null;
    }
}
