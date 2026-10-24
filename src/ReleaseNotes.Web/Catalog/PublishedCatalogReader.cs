using ReleaseNotes.Core.Catalog;
using ReleaseNotes.Core.Versioning;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using Types = ReleaseNotes.Web.ContentModel.ContentTypes;

namespace ReleaseNotes.Web.Catalog;

// Maps the published content cache to Core records. Drafts are not in that cache (outside preview),
// so they cannot leak into the site or the API. Scoped, so the tree is read once per request.
public sealed class PublishedCatalogReader
{
    private readonly IDocumentNavigationQueryService _navigation;
    private readonly IPublishedContentStatusFilteringService _publishedStatus;
    private readonly IPublishedValueFallback _valueFallback;
    private readonly IPublishedUrlProvider _urlProvider;
    private readonly ILogger<PublishedCatalogReader> _logger;
    private CatalogSnapshot? _snapshot;

    public PublishedCatalogReader(
        IDocumentNavigationQueryService navigation,
        IPublishedContentStatusFilteringService publishedStatus,
        IPublishedValueFallback valueFallback,
        IPublishedUrlProvider urlProvider,
        ILogger<PublishedCatalogReader> logger)
    {
        _navigation = navigation;
        _publishedStatus = publishedStatus;
        _valueFallback = valueFallback;
        _urlProvider = urlProvider;
        _logger = logger;
    }

    public CatalogSnapshot GetSnapshot() => _snapshot ??= ReadSnapshot();

    public IPublishedContent? GetHome() =>
        _navigation.TryGetRootKeysOfType(Types.Home.Alias, out IEnumerable<Guid> keys)
            ? _publishedStatus.FilterAvailable(keys, culture: null).FirstOrDefault()
            : null;

    private CatalogSnapshot ReadSnapshot()
    {
        IPublishedContent? home = GetHome();
        if (home is null)
        {
            return CatalogSnapshot.Empty;
        }

        IPublishedContent? catalog = ChildrenOfType(home, Types.ProductCatalog.Alias).FirstOrDefault();
        IPublishedContent? changelog = ChildrenOfType(home, Types.Changelog.Alias).FirstOrDefault();

        var products = new List<Product>();
        var releases = new List<Release>();

        foreach (IPublishedContent productContent in catalog is null ? [] : ChildrenOfType(catalog, Types.Product.Alias))
        {
            Product product = MapProduct(productContent);
            products.Add(product);

            foreach (IPublishedContent releaseContent in ChildrenOfType(productContent, Types.Release.Alias))
            {
                if (TryMapRelease(releaseContent, product.ToReference()) is { } release)
                {
                    releases.Add(release);
                }
            }
        }

        var links = new SiteLinks(Url(home), catalog is null ? null : Url(catalog), changelog is null ? null : Url(changelog));
        return new CatalogSnapshot(products, releases, links);
    }

    private IEnumerable<IPublishedContent> ChildrenOfType(IPublishedContent parent, string alias) =>
        _navigation.TryGetChildrenKeysOfType(parent.Key, alias, out IEnumerable<Guid> keys)
            ? _publishedStatus.FilterAvailable(keys, culture: null)
            : [];

    private Product MapProduct(IPublishedContent content)
    {
        var url = Url(content);
        IPublishedContent? logo = Value<IPublishedContent>(content, Types.Product.Logo);

        return new Product(
            Key: content.Key,
            Name: content.Name,
            Slug: content.UrlSegment ?? url.Trim('/').Split('/').Last(),
            Url: url,
            Tagline: Value<string>(content, Types.Product.Tagline) ?? string.Empty,
            DescriptionHtml: Html(content, Types.Product.Description),
            LogoUrl: logo is null ? null : _urlProvider.GetMediaUrl(logo),
            WebsiteUrl: Text(content, Types.Product.WebsiteUrl),
            RepositoryUrl: Text(content, Types.Product.RepositoryUrl),
            Lifecycle: ProductLifecycles.ParseOrDefault(Value<string>(content, Types.Product.Lifecycle)),
            IsFeatured: Value<bool>(content, Types.Product.IsFeatured),
            UpdatedAt: content.UpdateDate);
    }

    private Release? TryMapRelease(IPublishedContent content, ProductReference product)
    {
        var rawVersion = Value<string>(content, Types.Release.Version);
        DateOnly? date = Value<DateOnly?>(content, Types.Release.ReleaseDate);

        // The save handler rejects these, but content created before a rule existed must not break the site.
        if (!SemanticVersion.TryParse(rawVersion, out SemanticVersion? version) || date is null)
        {
            _logger.LogWarning("Release {ReleaseKey} skipped: invalid version '{Version}' or missing date", content.Key, rawVersion);
            return null;
        }

        if (!ReleaseTypes.TryParseLabel(Value<string>(content, Types.Release.ReleaseType), out ReleaseType type))
        {
            type = ReleaseType.Patch;
        }

        return new Release(
            Key: content.Key,
            Product: product,
            Version: version,
            ReleaseDate: date.Value,
            Type: type,
            Summary: Value<string>(content, Types.Release.Summary) ?? string.Empty,
            MarkedAsBreaking: Value<bool>(content, Types.Release.HasBreakingChanges),
            Url: Url(content),
            ExternalUrl: Text(content, Types.Release.ExternalUrl),
            Notes: MapNotes(Value<BlockListModel>(content, Types.Release.Notes)),
            UpdatedAt: content.UpdateDate);
    }

    private List<ReleaseNote> MapNotes(BlockListModel? blocks)
    {
        var notes = new List<ReleaseNote>();
        foreach (IPublishedElement element in blocks?.Select(block => block.Content) ?? [])
        {
            if (element.ContentType.Alias != Types.ReleaseNote.Alias
                || !ChangeCategories.TryParseLabel(Value<string>(element, Types.ReleaseNote.Category), out ChangeCategory category))
            {
                continue;
            }

            notes.Add(new ReleaseNote(
                Value<string>(element, Types.ReleaseNote.Title) ?? string.Empty,
                category,
                Html(element, Types.ReleaseNote.Description)));
        }

        return notes;
    }

    private T? Value<T>(IPublishedElement element, string alias) => element.Value<T>(_valueFallback, alias);

    private string? Text(IPublishedElement element, string alias) => NullIfEmpty(Value<string>(element, alias));

    private string? Html(IPublishedElement element, string alias) =>
        NullIfEmpty(Value<IHtmlEncodedString>(element, alias)?.ToHtmlString());

    private string Url(IPublishedContent content) => _urlProvider.GetUrl(content);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
