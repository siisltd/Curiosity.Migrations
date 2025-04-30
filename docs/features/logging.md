## Logging

## General Logging

Curiosity.Migrations provides logging capabilities to monitor the migration process. You can specify a logger that will track all migration events, including database creation, table creation, migration execution, errors, and warnings.

To enable general logging:

```csharp
using Microsoft.Extensions.Logging;

// Create a logger (e.g., using ILoggerFactory)
ILogger logger = loggerFactory.CreateLogger("Migrations");

// Configure the migration engine with the logger
var migrationEngine = new MigrationEngineBuilder()
    // ... other configuration
    .UseLogger(logger)
    .Build();
```

The general logger tracks major events like:
- Database existence checks and creation
- Migration table checks and creation
- Migration execution progress and errors
- Pre-migration script execution
- Summary of applied migrations

## SQL Query Logging

Curiosity.Migrations also provides a separate logging mechanism specifically for SQL queries executed during migrations. This allows you to track all SQL statements executed against your database.

```csharp
using Microsoft.Extensions.Logging;

// Create a SQL-specific logger
ILogger sqlLogger = loggerFactory.CreateLogger("MigrationsSql");

// Configure the migration engine with the SQL logger
var migrationEngine = new MigrationEngineBuilder()
    // ... other configuration
    .UseLoggerForSql(sqlLogger)
    .Build();
```

The SQL logger captures:
- Complete SQL query text
- Parameter names and values
- Execution timing

This is particularly useful for:
- Debugging database issues during migrations
- Auditing database changes
- Performance analysis of SQL operations
- Verifying that generated SQL matches expected queries

By using separate loggers for general migration events and SQL queries, you can control the verbosity of each independently based on your needs.

## Using Logger in Code Migrations

When implementing code migrations, you have direct access to the logger configured for the migration engine. The logger is available through the `Logger` property in your code migration class.

Here's how to use the logger in your code migrations:

```csharp
public class UserDataMigration : CodeMigration
{
    public override MigrationVersion Version => new MigrationVersion(1, 0);
    public override string? Comment => "User data migration";

    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // Log informational messages
        Logger?.LogInformation("Starting user data migration");
        
        // Execute migration logic
        var sql = "SELECT COUNT(*) FROM Users";
        var count = await MigrationConnection.ExecuteScalarSqlAsync(sql, null, cancellationToken);
        
        // Log results
        Logger?.LogInformation($"Found {count} users to process");
        
        // Log warnings if needed
        if (Convert.ToInt32(count) > 1000)
        {
            Logger?.LogWarning("Large number of users may cause migration to take longer");
        }
        
        try
        {
            // Perform migration operations
            // ...
            
            Logger?.LogInformation("User data migration completed successfully");
        }
        catch (Exception ex)
        {
            // Log errors
            Logger?.LogError(ex, "Error during user data migration");
            throw; // Re-throw to let migration engine handle the error
        }
    }
}
```

The `Logger` property is nullable, which means it might be `null` if no logger was configured when creating the migration engine. Always use the null-conditional operator (`?.`) when accessing it to avoid `NullReferenceException`.

Common logging patterns in code migrations include:

- Logging the beginning and end of major operations
- Tracking counts of processed records
- Warning about potential performance issues
- Reporting progress during long-running migrations
- Recording detailed error information during exceptions