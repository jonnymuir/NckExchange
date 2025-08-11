using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using NckExchange.Core.Migrations; // Ensure this namespace is correct
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Notifications; // Required for INotificationHandler

namespace NckExchange.Core.Composers;

public class MigrationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register a notification handler to run the migration plan on application startup
        builder.AddNotificationHandler<UmbracoApplicationStartingNotification, RunContactMessagesMigrationPlan>();
    }
}

// Notification handler to execute the migration plan
public class RunContactMessagesMigrationPlan(
    IMigrationPlanExecutor migrationPlanExecutor,
    ICoreScopeProvider coreScopeProvider,
    IKeyValueService keyValueService,
    ILogger<RunContactMessagesMigrationPlan> logger,
    IRuntimeState runtimeState) : INotificationHandler<UmbracoApplicationStartingNotification>
{

    public void Handle(UmbracoApplicationStartingNotification notification)
    {
        // Only run migrations if Umbraco is in a "Run" state (i.e., not installing or upgrading)
        if (runtimeState.Level < RuntimeLevel.Run)
        {
            return;
        }

        // Define the migration plan for your feature
        var migrationPlan = new MigrationPlan("ContactMessagesMigration");
        migrationPlan.From(string.Empty) // Start from an empty state
            .To<AddContactMessagesTable>("contactmessages-table-created"); // Execute our migration

        // Create the upgrader and execute the plan
        var upgrader = new Upgrader(migrationPlan);
        upgrader.Execute(migrationPlanExecutor, coreScopeProvider, keyValueService);

        logger.LogInformation("ContactMessages migration plan executed successfully.");
    }
}