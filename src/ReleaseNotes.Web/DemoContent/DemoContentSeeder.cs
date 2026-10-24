using System.Text;
using Microsoft.Extensions.Options;
using ReleaseNotes.Core.Catalog;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.PropertyEditors;
using Umbraco.Extensions;
using Types = ReleaseNotes.Web.ContentModel.ContentTypes;

namespace ReleaseNotes.Web.DemoContent;

public sealed class DemoContentOptions
{
    public const string SectionName = "ReleaseNotes:DemoContent";

    public bool Enabled { get; set; }
}

// Runs after startup, not as a migration: notifications are suppressed during migrations, and the
// URL table and search indexes are maintained by notification handlers. Only seeds an empty tree,
// once per database.
public sealed class DemoContentSeeder : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private const string SeededMarkerKey = "ReleaseNotes.DemoContent.SeededAt";

    private readonly IRuntimeState _runtimeState;
    private readonly IKeyValueService _keyValueService;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly MediaFileManager _mediaFileManager;
    private readonly MediaUrlGeneratorCollection _mediaUrlGenerators;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly IContentTypeBaseServiceProvider _contentTypeBaseServiceProvider;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly IOptions<DemoContentOptions> _options;
    private readonly ILogger<DemoContentSeeder> _logger;

    public DemoContentSeeder(
        IRuntimeState runtimeState,
        IKeyValueService keyValueService,
        IContentService contentService,
        IMediaService mediaService,
        MediaFileManager mediaFileManager,
        MediaUrlGeneratorCollection mediaUrlGenerators,
        IShortStringHelper shortStringHelper,
        IContentTypeBaseServiceProvider contentTypeBaseServiceProvider,
        IJsonSerializer jsonSerializer,
        IOptions<DemoContentOptions> options,
        ILogger<DemoContentSeeder> logger)
    {
        _runtimeState = runtimeState;
        _keyValueService = keyValueService;
        _contentService = contentService;
        _mediaService = mediaService;
        _mediaFileManager = mediaFileManager;
        _mediaUrlGenerators = mediaUrlGenerators;
        _shortStringHelper = shortStringHelper;
        _contentTypeBaseServiceProvider = contentTypeBaseServiceProvider;
        _jsonSerializer = jsonSerializer;
        _options = options;
        _logger = logger;
    }

    public Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled || _runtimeState.Level != RuntimeLevel.Run)
        {
            return Task.CompletedTask;
        }

        if (_keyValueService.GetValue(SeededMarkerKey) is { } seededAt)
        {
            _logger.LogDebug("Demo content was already created at {SeededAt}; skipping", seededAt);
            return Task.CompletedTask;
        }

        if (_contentService.GetRootContent().Any())
        {
            _logger.LogInformation("Demo content skipped: the content tree is not empty");
            return Task.CompletedTask;
        }

        Seed();
        _keyValueService.SetValue(SeededMarkerKey, DateTime.UtcNow.ToString("O"));
        return Task.CompletedTask;
    }

    private void Seed()
    {
        _logger.LogInformation("Creating demo content for {ProductCount} products", DemoCatalog.Products.Count);

        IContent home = CreateAndPublish(DemoCatalog.HomeName, Constants.System.Root, Types.Home.Alias, content =>
        {
            content.SetValue(Types.Home.HeroTitle, "Every change, clearly told.");
            content.SetValue(Types.Home.HeroText, "Release notes for the developer tools we build: what is new, what is fixed and what you need to change before you upgrade.");
            content.SetValue(Types.SeoSettings.MetaDescription, "Changelogs and release notes for ForgeKit, Lumen and Relay.");
        });

        IContent catalog = CreateAndPublish("Products", home.Id, Types.ProductCatalog.Alias, content =>
            content.SetValue(Types.ProductCatalog.Intro, "Developer tools with a public, versioned history of every change."));

        CreateAndPublish("Changelog", home.Id, Types.Changelog.Alias, content =>
            content.SetValue(Types.Changelog.Intro, "Every release across all products, newest first. Filter by product, type of change or period."));

        IMedia mediaFolder = _mediaService.CreateMedia(DemoCatalog.MediaFolderName, Constants.System.Root, Constants.Conventions.MediaTypes.Folder);
        _mediaService.Save(mediaFolder);

        foreach (DemoProduct demoProduct in DemoCatalog.Products)
        {
            IMedia logo = CreateLogo(mediaFolder, demoProduct);

            IContent product = CreateAndPublish(demoProduct.Name, catalog.Id, Types.Product.Alias, content =>
            {
                content.SetValue(Types.Product.Tagline, demoProduct.Tagline);
                content.SetValue(Types.Product.Description, RichText(demoProduct.DescriptionHtml));
                content.SetValue(Types.Product.Logo, _jsonSerializer.Serialize(new[] { new { key = Guid.NewGuid(), mediaKey = logo.Key } }));
                content.SetValue(Types.Product.WebsiteUrl, demoProduct.WebsiteUrl);
                content.SetValue(Types.Product.RepositoryUrl, demoProduct.RepositoryUrl);
                content.SetValue(Types.Product.Lifecycle, Dropdown(demoProduct.Lifecycle.ToString()));
                content.SetValue(Types.Product.IsFeatured, demoProduct.IsFeatured);
            });

            foreach (DemoRelease demoRelease in demoProduct.Releases)
            {
                CreateRelease(product, demoRelease);
            }
        }
    }

    private void CreateRelease(IContent product, DemoRelease demoRelease)
    {
        void Configure(IContent content)
        {
            content.SetValue(Types.Release.Version, demoRelease.Version);
            content.SetValue(Types.Release.ReleaseDate, _jsonSerializer.Serialize(new DateTimeValueConverterBase.DateTimeDto
            {
                Date = new DateTimeOffset(demoRelease.Date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            }));
            content.SetValue(Types.Release.ReleaseType, Dropdown(demoRelease.Type.ToString()));
            content.SetValue(Types.Release.Summary, demoRelease.Summary);
            content.SetValue(Types.Release.HasBreakingChanges, demoRelease.Breaking);
            content.SetValue(Types.Release.ExternalUrl, demoRelease.ExternalUrl);
            content.SetValue(Types.Release.Notes, Notes(demoRelease.Notes));
        }

        if (demoRelease.Publish)
        {
            CreateAndPublish(demoRelease.Version, product.Id, Types.Release.Alias, Configure);
            return;
        }

        IContent draft = _contentService.Create(demoRelease.Version, product.Id, Types.Release.Alias);
        Configure(draft);
        _contentService.Save(draft);
    }

    private IContent CreateAndPublish(string name, int parentId, string contentTypeAlias, Action<IContent> configure)
    {
        IContent content = _contentService.Create(name, parentId, contentTypeAlias);
        configure(content);

        OperationResult saved = _contentService.Save(content);
        if (!saved.Success)
        {
            throw new InvalidOperationException($"Demo content '{name}' could not be saved: {saved.Result}.");
        }

        PublishResult published = _contentService.Publish(content, ["*"]);
        return published.Success
            ? content
            : throw new InvalidOperationException($"Demo content '{name}' could not be published: {published.Result}.");
    }

    private IMedia CreateLogo(IMedia folder, DemoProduct product)
    {
        IMedia media = _mediaService.CreateMedia($"{product.Name} logo", folder, Constants.Conventions.MediaTypes.VectorGraphicsAlias);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(product.LogoSvg));
        media.SetValue(
            _mediaFileManager,
            _mediaUrlGenerators,
            _shortStringHelper,
            _contentTypeBaseServiceProvider,
            Constants.Conventions.Media.File,
            $"{product.Name.ToLowerInvariant()}.svg",
            stream);
        _mediaService.Save(media);
        return media;
    }

    private string Notes(IEnumerable<DemoNote> notes)
    {
        var contentData = new List<BlockItemData>();
        var layout = new List<BlockListLayoutItem>();

        foreach (DemoNote note in notes)
        {
            var key = Guid.NewGuid();
            contentData.Add(new BlockItemData(key, Types.ReleaseNote.Key, Types.ReleaseNote.Alias)
            {
                Values =
                [
                    new BlockPropertyValue { Alias = Types.ReleaseNote.Title, Value = note.Title },
                    new BlockPropertyValue { Alias = Types.ReleaseNote.Category, Value = Dropdown(note.Category.Label()) },
                    new BlockPropertyValue { Alias = Types.ReleaseNote.Description, Value = note.DescriptionHtml is null ? null : RichText(note.DescriptionHtml) },
                ],
            });
            layout.Add(new BlockListLayoutItem(key));
        }

        var value = new BlockListValue(layout)
        {
            ContentData = contentData,
            Expose = contentData.Select(item => new BlockItemVariation(item.Key, culture: null, segment: null)).ToList(),
        };
        return _jsonSerializer.Serialize(value);
    }

    // Same storage formats the backoffice property editors write.
    private string Dropdown(string value) => _jsonSerializer.Serialize(new[] { value });

    private string RichText(string html) =>
        RichTextPropertyEditorHelper.SerializeRichTextEditorValue(new RichTextEditorValue { Markup = html, Blocks = null }, _jsonSerializer);
}
