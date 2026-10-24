using System.Text.Json;
using ReleaseNotes.Core.Catalog;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;

namespace ReleaseNotes.Web.ContentModel;

// A saving handler rather than property validation, so the rules also apply to saves from the
// Management API and from code.
public sealed class ReleaseSavingHandler : INotificationHandler<ContentSavingNotification>
{
    private const int MaxSiblings = 1000;

    private readonly IContentService _contentService;

    public ReleaseSavingHandler(IContentService contentService) => _contentService = contentService;

    public void Handle(ContentSavingNotification notification)
    {
        foreach (IContent release in notification.SavedEntities.Where(e => e.ContentType.Alias == ContentTypes.Release.Alias))
        {
            IEnumerable<string?> otherVersions = release.ParentId > 0
                ? _contentService
                    .GetPagedChildren(
                        release.ParentId,
                        pageIndex: 0,
                        MaxSiblings,
                        out _,
                        propertyAliases: [ContentTypes.Release.Version],
                        filter: null,
                        ordering: null,
                        loadTemplates: false)
                    .Where(sibling => sibling.Key != release.Key && sibling.ContentType.Alias == ContentTypes.Release.Alias)
                    .Select(sibling => sibling.GetValue<string>(ContentTypes.Release.Version))
                : [];

            ReleaseVersionCheck check = ReleaseVersionCheck.Run(
                release.GetValue<string>(ContentTypes.Release.Version),
                ReadDropdownValue(release.GetValue<string>(ContentTypes.Release.ReleaseType)),
                otherVersions);

            if (!check.IsValid)
            {
                notification.CancelOperation(new EventMessage("Release version", check.Error!, EventMessageType.Error));
                return;
            }

            // "v1.4.0 " is stored as "1.4.0".
            var normalized = check.Version!.ToString();
            if (release.GetValue<string>(ContentTypes.Release.Version) != normalized)
            {
                release.SetValue(ContentTypes.Release.Version, normalized);
            }

            if (check.Warning is not null)
            {
                notification.Messages.Add(new EventMessage("Release type", check.Warning, EventMessageType.Warning));
            }
        }
    }

    // The dropdown stores a JSON array, e.g. ["Minor"].
    internal static string? ReadDropdownValue(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(stored)?.FirstOrDefault();
        }
        catch (JsonException)
        {
            return stored;
        }
    }
}
