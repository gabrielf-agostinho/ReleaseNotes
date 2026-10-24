using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;

namespace ReleaseNotes.Web.ContentModel.Migrations;

// Umbraco keeps the plan state in its key/value table, so each step runs once per database.
// Schema changes go in as new steps.
public sealed class ContentModelMigrationRunner : INotificationAsyncHandler<UmbracoApplicationStartingNotification>
{
    public const string PlanName = "ReleaseNotes.ContentModel";

    private readonly IMigrationPlanExecutor _migrationPlanExecutor;
    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IKeyValueService _keyValueService;
    private readonly IRuntimeState _runtimeState;
    private readonly ILogger<ContentModelMigrationRunner> _logger;

    public ContentModelMigrationRunner(
        IMigrationPlanExecutor migrationPlanExecutor,
        ICoreScopeProvider scopeProvider,
        IKeyValueService keyValueService,
        IRuntimeState runtimeState,
        ILogger<ContentModelMigrationRunner> logger)
    {
        _migrationPlanExecutor = migrationPlanExecutor;
        _scopeProvider = scopeProvider;
        _keyValueService = keyValueService;
        _runtimeState = runtimeState;
        _logger = logger;
    }

    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        // Before installation completes there is no database to migrate; the installer restarts the
        // runtime afterwards and this handler runs again.
        if (_runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        var plan = new MigrationPlan(PlanName);
        plan.From(string.Empty)
            .To<CreateContentModel>("release-notes-content-model-v1");

        ExecutedMigrationPlan result = await new Upgrader(plan)
            .ExecuteAsync(_migrationPlanExecutor, _scopeProvider, _keyValueService);

        if (!result.Successful)
        {
            throw new InvalidOperationException($"Migration plan '{PlanName}' failed at state '{result.FinalState}'.", result.Exception);
        }

        if (result.CompletedTransitions.Count > 0)
        {
            _logger.LogInformation("Content model migrated to {State}", result.FinalState);
        }
    }
}
