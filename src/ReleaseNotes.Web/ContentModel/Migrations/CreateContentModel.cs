using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using static ReleaseNotes.Web.ContentModel.ContentTypes;
using ChangeCategories = ReleaseNotes.Core.Catalog.ChangeCategories;
using ProductLifecycles = ReleaseNotes.Core.Catalog.ProductLifecycles;
using ReleaseTypes = ReleaseNotes.Core.Catalog.ReleaseTypes;

namespace ReleaseNotes.Web.ContentModel.Migrations;

// Goes through the regular services rather than SQL, so the result is the same as building the model
// by hand in the backoffice.
public sealed class CreateContentModel : AsyncMigrationBase
{
    // Same rules as SemanticVersion.TryParse; the save handler strips the "v".
    internal const string VersionPattern =
        @"^[vV]?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$";

    private const string UrlPattern = @"^https?://[^\s]+$";
    private const string SlugPattern = "^[a-z0-9]+(-[a-z0-9]+)*$";

    private readonly IContentTypeService _contentTypeService;
    private readonly IDataTypeService _dataTypeService;
    private readonly PropertyEditorCollection _propertyEditors;
    private readonly IConfigurationEditorJsonSerializer _configurationSerializer;
    private readonly IShortStringHelper _shortStringHelper;

    public CreateContentModel(
        IMigrationContext context,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        PropertyEditorCollection propertyEditors,
        IConfigurationEditorJsonSerializer configurationSerializer,
        IShortStringHelper shortStringHelper)
        : base(context)
    {
        _contentTypeService = contentTypeService;
        _dataTypeService = dataTypeService;
        _propertyEditors = propertyEditors;
        _configurationSerializer = configurationSerializer;
        _shortStringHelper = shortStringHelper;
    }

    protected override async Task MigrateAsync()
    {
        // Notifications are suppressed while migrations run, so ask for a cache rebuild afterwards.
        RebuildCache = true;

        IDataType textstring = await GetBuiltInDataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);
        IDataType textarea = await GetBuiltInDataTypeAsync(Constants.DataTypes.Guids.TextareaGuid);
        IDataType richText = await GetBuiltInDataTypeAsync(Constants.DataTypes.Guids.RichtextEditorGuid);
        IDataType toggle = await GetBuiltInDataTypeAsync(Constants.DataTypes.Guids.CheckboxGuid);

        IDataType releaseDate = await EnsureDataTypeAsync(
            DataTypeKeys.ReleaseDate, "Release Notes - Release Date", Constants.PropertyEditors.Aliases.DateOnly, "Umb.PropertyEditorUi.DateOnlyPicker");
        IDataType releaseType = await EnsureDropdownAsync(DataTypeKeys.ReleaseType, "Release Notes - Release Type", ReleaseTypes.Labels);
        IDataType changeCategory = await EnsureDropdownAsync(DataTypeKeys.ChangeCategory, "Release Notes - Change Category", ChangeCategories.Labels);
        IDataType lifecycle = await EnsureDropdownAsync(DataTypeKeys.ProductLifecycle, "Release Notes - Product Lifecycle", ProductLifecycles.Labels);
        IDataType logo = await EnsureDataTypeAsync(
            DataTypeKeys.ProductLogo,
            "Release Notes - Product Logo",
            Constants.PropertyEditors.Aliases.MediaPicker3,
            "Umb.PropertyEditorUi.MediaPicker",
            new Dictionary<string, object>
            {
                ["filter"] = $"{Constants.MediaTypes.Guids.Image},{Constants.MediaTypes.Guids.Svg}",
                ["multiple"] = false,
            });

        // Element type first: the block list data type references it.
        await EnsureContentTypeAsync(ReleaseNote.Key, ReleaseNote.Alias, "Release Note", "icon-bulleted-list color-indigo", type =>
        {
            type.IsElement = true;
            type.Description = "A single change inside a release. Order the blocks to set the display order.";
            AddProperty(type, "content", "Change", textstring, ReleaseNote.Title, "Title", p =>
            {
                p.Mandatory = true;
                p.Description = "One line, written for the people who use the product.";
            });
            AddProperty(type, "content", "Change", changeCategory, ReleaseNote.Category, "Category", p => p.Mandatory = true);
            AddProperty(type, "content", "Change", richText, ReleaseNote.Description, "Description", p =>
                p.Description = "Optional details: context, migration steps, links.");
        });

        IDataType notes = await EnsureDataTypeAsync(
            DataTypeKeys.ReleaseNotes,
            "Release Notes - Notes",
            Constants.PropertyEditors.Aliases.BlockList,
            "Umb.PropertyEditorUi.BlockList",
            new Dictionary<string, object>
            {
                ["blocks"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["contentElementTypeKey"] = ReleaseNote.Key,
                        ["label"] = "{umbValue: category} · {umbValue: title}",
                    },
                },
            });

        IContentType seo = await EnsureContentTypeAsync(SeoSettings.Key, SeoSettings.Alias, "SEO Settings", "icon-search", type =>
        {
            type.Description = "Composition: optional search engine and social sharing overrides.";
            AddProperty(type, "seo", "SEO", textstring, SeoSettings.MetaTitle, "Meta title", p =>
                p.Description = "Overrides the page title in search results. Leave empty to use the page name.");
            AddProperty(type, "seo", "SEO", textarea, SeoSettings.MetaDescription, "Meta description", p =>
                p.Description = "Around 150 characters. Leave empty to use the page summary.");
        });

        await EnsureContentTypeAsync(Release.Key, Release.Alias, "Release", "icon-tag color-indigo", type =>
        {
            type.Description = "A published version of a product. Created below a product.";
            type.AddContentType(seo);
            AddProperty(type, "release", "Release", textstring, Release.Version, "Version", p =>
            {
                p.Mandatory = true;
                p.ValidationRegExp = VersionPattern;
                p.ValidationRegExpMessage = "Use semantic versioning, for example 1.4.0 or 2.0.0-rc.1.";
                p.Description = "Semantic version, e.g. 1.4.0. Also used for the page URL.";
            });
            AddProperty(type, "release", "Release", releaseDate, Release.ReleaseDate, "Release date", p => p.Mandatory = true);
            AddProperty(type, "release", "Release", releaseType, Release.ReleaseType, "Release type", p => p.Mandatory = true);
            AddProperty(type, "release", "Release", textarea, Release.Summary, "Summary", p =>
            {
                p.Mandatory = true;
                p.Description = "One or two sentences shown in timelines and feeds.";
            });
            AddProperty(type, "release", "Release", toggle, Release.HasBreakingChanges, "Contains breaking changes", p =>
                p.Description = "Highlights the release as incompatible. Also implied by any note in the Breaking Change category.");
            AddProperty(type, "release", "Release", textstring, Release.ExternalUrl, "External release URL", p =>
            {
                RequireHttpUrl(p);
                p.Description = "Optional link to a GitHub release, migration guide or documentation.";
            });
            AddProperty(type, "notes", "Notes", notes, Release.Notes, "Release notes", p =>
                p.Description = "Add one block per change. The order here is the display order within each category.");
        });

        await EnsureContentTypeAsync(Product.Key, Product.Alias, "Product", "icon-box color-indigo", type =>
        {
            type.Description = "A software product. Its releases are created below it.";
            type.AddContentType(seo);
            type.AllowedContentTypes = [new ContentTypeSort(Release.Key, 0, Release.Alias)];
            type.ListView = Constants.DataTypes.Guids.ListViewContentGuid;
            AddProperty(type, "product", "Product", textarea, Product.Tagline, "Short description", p =>
            {
                p.Mandatory = true;
                p.Description = "One sentence shown on cards and in search results.";
            });
            AddProperty(type, "product", "Product", richText, Product.Description, "Full description");
            AddProperty(type, "product", "Product", logo, Product.Logo, "Logo", p =>
                p.Description = "Square image or SVG. A monogram is shown when empty.");
            AddProperty(type, "product", "Product", lifecycle, Product.Lifecycle, "Lifecycle", p =>
                p.Description = "Availability shown to readers. Unpublishing hides the product entirely.");
            AddProperty(type, "product", "Product", toggle, Product.IsFeatured, "Featured on the home page");
            AddProperty(type, "links", "Links", textstring, Product.WebsiteUrl, "Website", RequireHttpUrl);
            AddProperty(type, "links", "Links", textstring, Product.RepositoryUrl, "Repository", RequireHttpUrl);
            AddProperty(type, "links", "Links", textstring, Product.Slug, "URL slug", p =>
            {
                p.ValidationRegExp = SlugPattern;
                p.ValidationRegExpMessage = "Lowercase letters, numbers and single hyphens only.";
                p.Description = "Optional. Overrides the URL segment generated from the name.";
            });
        });

        await EnsureContentTypeAsync(ProductCatalog.Key, ProductCatalog.Alias, "Product Catalog", "icon-folder color-indigo", type =>
        {
            type.Description = "Lists all published products.";
            type.AddContentType(seo);
            type.AllowedContentTypes = [new ContentTypeSort(Product.Key, 0, Product.Alias)];
            AddProperty(type, "content", "Content", textarea, ProductCatalog.Intro, "Introduction");
        });

        await EnsureContentTypeAsync(Changelog.Key, Changelog.Alias, "Changelog", "icon-time color-indigo", type =>
        {
            type.Description = "The release history across all products, with filters.";
            type.AddContentType(seo);
            AddProperty(type, "content", "Content", textarea, Changelog.Intro, "Introduction");
        });

        await EnsureContentTypeAsync(Home.Key, Home.Alias, "Home", "icon-home color-indigo", type =>
        {
            type.Description = "The site root.";
            type.AllowedAsRoot = true;
            type.AddContentType(seo);
            type.AllowedContentTypes =
            [
                new ContentTypeSort(ProductCatalog.Key, 0, ProductCatalog.Alias),
                new ContentTypeSort(Changelog.Key, 1, Changelog.Alias),
            ];
            AddProperty(type, "content", "Content", textstring, Home.HeroTitle, "Hero title", p => p.Mandatory = true);
            AddProperty(type, "content", "Content", textarea, Home.HeroText, "Hero text");
        });
    }

    private void AddProperty(
        IContentType type,
        string groupAlias,
        string groupName,
        IDataType dataType,
        string alias,
        string name,
        Action<PropertyType>? configure = null)
    {
        var property = new PropertyType(_shortStringHelper, dataType, alias) { Name = name };
        configure?.Invoke(property);
        type.AddPropertyType(property, groupAlias, groupName);
    }

    private static void RequireHttpUrl(PropertyType property)
    {
        property.ValidationRegExp = UrlPattern;
        property.ValidationRegExpMessage = "Enter an absolute http(s) URL.";
    }

    private async Task<IDataType> GetBuiltInDataTypeAsync(Guid key) =>
        await _dataTypeService.GetAsync(key)
        ?? throw new InvalidOperationException(
            $"Built-in data type {key} is missing. The release notes content model relies on Umbraco's default data types.");

    private Task<IDataType> EnsureDropdownAsync(Guid key, string name, IEnumerable<string> items) =>
        EnsureDataTypeAsync(
            key,
            name,
            Constants.PropertyEditors.Aliases.DropDownListFlexible,
            "Umb.PropertyEditorUi.Dropdown",
            new Dictionary<string, object> { ["items"] = items.ToArray(), ["multiple"] = false });

    private async Task<IDataType> EnsureDataTypeAsync(
        Guid key,
        string name,
        string editorAlias,
        string editorUiAlias,
        IDictionary<string, object>? configuration = null)
    {
        if (await _dataTypeService.GetAsync(key) is { } existing)
        {
            return existing;
        }

        if (!_propertyEditors.TryGet(editorAlias, out IDataEditor? editor))
        {
            throw new InvalidOperationException($"Property editor '{editorAlias}' is not registered.");
        }

        var dataType = new DataType(editor, _configurationSerializer)
        {
            Key = key,
            Name = name,
            EditorUiAlias = editorUiAlias,
            DatabaseType = ValueTypes.ToStorageType(editor.GetValueEditor().ValueType),
        };

        if (configuration is not null)
        {
            dataType.ConfigurationData = configuration;
        }

        Attempt<IDataType, DataTypeOperationStatus> result = await _dataTypeService.CreateAsync(dataType, Constants.Security.SuperUserKey);
        return result.Success
            ? result.Result
            : throw new InvalidOperationException($"Could not create data type '{name}': {result.Status}.");
    }

    private async Task<IContentType> EnsureContentTypeAsync(
        Guid key,
        string alias,
        string name,
        string icon,
        Action<IContentType> configure)
    {
        if (await _contentTypeService.GetAsync(key) is { } existing)
        {
            return existing;
        }

        var contentType = new ContentType(_shortStringHelper, Constants.System.Root)
        {
            Key = key,
            Alias = alias,
            Name = name,
            Icon = icon,
        };
        configure(contentType);

        // Keep groups in the order they were added, with the SEO composition's group last.
        var sortOrder = key == SeoSettings.Key ? 100 : 0;
        foreach (PropertyGroup group in contentType.PropertyGroups)
        {
            group.SortOrder = sortOrder++;
        }

        Attempt<ContentTypeOperationStatus> result = await _contentTypeService.CreateAsync(contentType, Constants.Security.SuperUserKey);
        return result.Success
            ? contentType
            : throw new InvalidOperationException($"Could not create document type '{alias}': {result.Result}.");
    }
}
