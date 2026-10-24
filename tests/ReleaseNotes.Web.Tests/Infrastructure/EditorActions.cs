using Microsoft.Extensions.DependencyInjection;
using ReleaseNotes.Web.ContentModel;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;

namespace ReleaseNotes.Web.Tests.Infrastructure;

// Uses the same content service as the backoffice, so the real save handlers and cache updates run.
public sealed class EditorActions(ReleaseNotesSite site)
{
    private IContentService ContentService => site.Services.GetRequiredService<IContentService>();

    public IContent Product(string name) =>
        ContentService.GetRootContent()
            .SelectMany(root => Descendants(root.Id))
            .Single(content => content.ContentType.Alias == ContentTypes.Product.Alias && content.Name == name);

    public IContent Release(string productName, string version) =>
        Descendants(Product(productName).Id)
            .Single(content => content.GetValue<string>(ContentTypes.Release.Version) == version);

    public (IContent Release, OperationResult Result) SaveRelease(string productName, string version, string releaseType = "Minor")
    {
        IContent release = ContentService.Create(version, Product(productName).Id, ContentTypes.Release.Alias);
        release.SetValue(ContentTypes.Release.Version, version);
        release.SetValue(ContentTypes.Release.ReleaseDate, """{"date":"2026-10-20T00:00:00+00:00","timeZone":null}""");
        release.SetValue(ContentTypes.Release.ReleaseType, $"[\"{releaseType}\"]");
        release.SetValue(ContentTypes.Release.Summary, $"Integration test release {version}.");
        return (release, ContentService.Save(release));
    }

    public PublishResult Publish(IContent content) => ContentService.Publish(content, ["*"]);

    public PublishResult Unpublish(IContent content) => ContentService.Unpublish(content);

    public void Delete(IContent content) => ContentService.Delete(content);

    public int RootCount() => ContentService.GetRootContent().Count();

    private IEnumerable<IContent> Descendants(int id) =>
        ContentService.GetPagedDescendants(id, 0, 1000, out _);
}
