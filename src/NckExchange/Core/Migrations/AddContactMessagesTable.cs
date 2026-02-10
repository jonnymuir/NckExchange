using Umbraco.Cms.Infrastructure.Migrations;
using NckExchange.Core.Models;

namespace NckExchange.Core.Migrations;

public class AddContactMessagesTable(IMigrationContext context) : AsyncMigrationBase(context)
{
    protected override Task MigrateAsync()
    {
        Logger.LogDebug("Running migration {MigrationName}", nameof(AddContactMessagesTable));

        // Check if the table already exists to prevent errors on subsequent runs
        if (!TableExists("ContactMessages"))
        {
            Create.Table<ContactMessage>().Do();
            Logger.LogDebug("Created table {TableName}", "ContactMessages");
        }
        else
        {
            Logger.LogDebug("Table {TableName} already exists, skipping creation.", "ContactMessages");
        }

        return Task.CompletedTask;
    }
}
