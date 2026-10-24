using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ReleaseNotes.Web.Api;
using ReleaseNotes.Web.ContentModel;
using ReleaseNotes.Web.DemoContent;
using ReleaseNotes.Web.Tests.Infrastructure;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace ReleaseNotes.Web.Tests;

[Collection(SiteCollection.Name)]
public sealed class EditorialWorkflowTests(ReleaseNotesSite site)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client = site.CreateSiteClient();
    private readonly EditorActions _editor = new(site);

    private CancellationToken Cancellation => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_published_release_appears_on_the_site_and_in_the_api()
    {
        (IContent release, OperationResult saved) = _editor.SaveRelease("Relay", "0.3.0");
        try
        {
            Assert.True(saved.Success);
            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/products/relay/0-3-0/", Cancellation)).StatusCode);

            Assert.True(_editor.Publish(release).Success);

            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/products/relay/0-3-0/", Cancellation)).StatusCode);
            ReleaseDetailDto? latest = await _client.GetFromJsonAsync<ReleaseDetailDto>("/api/v1/products/relay/releases/latest", Json, Cancellation);
            Assert.Equal("0.3.0", latest?.Version);
        }
        finally
        {
            _editor.Delete(release);
        }

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/products/relay/0-3-0/", Cancellation)).StatusCode);
    }

    [Fact]
    public async Task An_unpublished_release_disappears_from_the_site_and_the_api()
    {
        IContent release = _editor.Release("Relay", "0.1.0");
        try
        {
            Assert.True(_editor.Unpublish(release).Success);

            Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/products/relay/0-1-0/", Cancellation)).StatusCode);
            PagedResponse<ReleaseSummaryDto>? relay = await _client.GetFromJsonAsync<PagedResponse<ReleaseSummaryDto>>("/api/v1/releases?product=relay", Json, Cancellation);
            Assert.Equal(["0.2.0"], relay?.Items.Select(r => r.Version));
        }
        finally
        {
            Assert.True(_editor.Publish(release).Success);
        }
    }

    [Fact]
    public void Saving_a_duplicate_version_is_cancelled()
    {
        (IContent _, OperationResult saved) = _editor.SaveRelease("ForgeKit", "1.4.0");

        Assert.False(saved.Success);
        Assert.Contains(saved.EventMessages?.GetAll() ?? [], message => message.Message.Contains("already exists", StringComparison.Ordinal));
    }

    [Fact]
    public void Saving_normalizes_the_version_and_warns_about_an_unexpected_release_type()
    {
        (IContent release, OperationResult saved) = _editor.SaveRelease("ForgeKit", "v2.0.0", releaseType: "Patch");
        try
        {
            Assert.True(saved.Success);
            Assert.Equal("2.0.0", release.GetValue<string>(ContentTypes.Release.Version));
            Assert.Contains(saved.EventMessages?.GetAll() ?? [], message => message.Message.Contains("major bump", StringComparison.Ordinal));
        }
        finally
        {
            _editor.Delete(release);
        }
    }

    [Fact]
    public async Task Demo_content_is_seeded_only_once()
    {
        var rootsBefore = _editor.RootCount();
        DemoContentSeeder seeder = ActivatorUtilities.CreateInstance<DemoContentSeeder>(site.Services);

        await seeder.HandleAsync(new UmbracoApplicationStartedNotification(false), Cancellation);

        Assert.Equal(1, rootsBefore);
        Assert.Equal(rootsBefore, _editor.RootCount());
    }

    [Fact]
    public void Content_model_is_created_by_the_migration()
    {
        IContentTypeService contentTypes = site.Services.GetRequiredService<IContentTypeService>();

        IContentType? release = contentTypes.Get(ContentTypes.Release.Key);
        IContentType? note = contentTypes.Get(ContentTypes.ReleaseNote.Key);

        Assert.NotNull(release);
        Assert.NotNull(note);
        Assert.True(note.IsElement);
        Assert.True(release.PropertyTypes.Single(p => p.Alias == ContentTypes.Release.Version).Mandatory);
        Assert.Contains(release.CompositionAliases(), alias => alias == ContentTypes.SeoSettings.Alias);
        Assert.Equal([ContentTypes.Release.Alias], contentTypes.Get(ContentTypes.Product.Key)!.AllowedContentTypes!.Select(t => t.Alias));
    }
}
