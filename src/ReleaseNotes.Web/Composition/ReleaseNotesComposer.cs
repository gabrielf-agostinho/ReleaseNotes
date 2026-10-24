using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using ReleaseNotes.Web.Api;
using ReleaseNotes.Web.Catalog;
using ReleaseNotes.Web.ContentModel;
using ReleaseNotes.Web.ContentModel.Migrations;
using ReleaseNotes.Web.DemoContent;
using ReleaseNotes.Web.Rendering.Controllers;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace ReleaseNotes.Web.Composition;

public sealed class ReleaseNotesComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, ContentModelMigrationRunner>();
        builder.AddNotificationHandler<ContentSavingNotification, ReleaseSavingHandler>();
        builder.UrlSegmentProviders().Insert<ReleaseUrlSegmentProvider>();

        builder.Services.AddScoped<PublishedCatalogReader>();
        builder.Services.AddScoped<SitePageDependencies>();
        builder.Services.ConfigureOptions<ChangelogApiSwaggerOptions>();
        builder.Services.AddHealthChecks();

        // Razor still encodes <, > and &; this stops it from turning every non-ASCII character
        // (·, –, accented names) into a numeric entity.
        builder.Services.Configure<WebEncoderOptions>(options => options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

        builder.Services.Configure<DemoContentOptions>(builder.Config.GetSection(DemoContentOptions.SectionName));
        builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, DemoContentSeeder>();
    }
}
