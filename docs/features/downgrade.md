# Downgrade Migrations

Downgrade migrations allow you to revert database changes to a previous state. This feature is essential for managing database schemas throughout an application's lifecycle, providing flexibility when deployment issues arise or when needing to roll back to a stable version.

## When to Use Downgrade Migrations

Downgrade migrations are particularly useful in the following scenarios:

1. **Deployment Rollbacks**: When a production deployment introduces issues, downgrade migrations enable you to quickly revert to the previous stable version.

2. **Testing Environments**: During development and testing, it's often necessary to move back and forth between database versions to test different features or fixes.

3. **Version Control**: When working with multiple branches, downgrade migrations make it easier to switch between different versions of your application.

4. **Phased Deployments**: For complex systems with staged rollouts, having the ability to roll back specific components is essential.

## Risks and Considerations

**Important**: Downgrade migrations can potentially lead to data loss. Be aware of the following risks:

1. **Data Loss**: Some schema changes cannot be reversed without losing data. For example:
   - Dropping columns that contain data
   - Changing column types that result in data truncation
   - Removing tables that contain important information

2. **Data Integrity**: Ensure that your downgrade scripts maintain referential integrity and don't leave the database in an inconsistent state.

3. **Testing**: Always thoroughly test your downgrade migrations in a non-production environment before relying on them in production.

4. **Backup**: Always create a database backup before performing any downgrade operation on a production system.

## How to Perform a Downgrade

To downgrade your database to a previous version:

1. **Configure the Migration Engine**:
   ```csharp
   var builder = new MigrationEngineBuilder(services)
       .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly())
       .UseScriptMigrations().FromDirectory("path/to/migrations")
       .ConfigureForSqlServer("YourConnectionString")
       .UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed)
       .SetUpTargetVersion(new MigrationVersion(1, 5)); // Target version to downgrade to
   
   var migrationEngine = builder.Build();
   ```

2. **Execute the Downgrade**:
   ```csharp
   var result = await migrationEngine.DowngradeDatabaseAsync();
   
   if (result.IsSuccessfully)
   {
       Console.WriteLine("Downgrade completed successfully");
   }
   else
   {
       Console.WriteLine($"Downgrade failed: {result.ErrorMessage}");
   }
   ```

The migration engine will:
- Identify migrations that have been applied and are higher than the target version
- Execute the `DowngradeAsync` method of each applicable migration in descending order (from highest to lowest)
- Remove each migration record from the journal after successful downgrade.

## Script migrations

To implement a downgrade-capable script migration:

1. Create an `.up.sql` file for the upgrade migration
2. Create a matching `.down.sql` file for the downgrade migration

The library will automatically pair these files based on their version numbers and create a `DowngradeScriptMigration` instance.

Script migrations follow this naming pattern:
- Upgrade: `<version>.up.sql` or `<version>.sql`
- Downgrade: `<version>.down.sql`

### Example

**1.0.up.sql**:
```sql
-- Migration to create Users table
CREATE TABLE Users (
    Id INT PRIMARY KEY,
    Name VARCHAR(100),
    Email VARCHAR(255)
);
```

**1.0.down.sql**:
```sql
-- Migration to drop Users table
DROP TABLE Users;
```

## Code migrations

To implement a downgrade-capable code migration:

1. Create a class that inherits from `CodeMigration` and implements the `IDowngradeMigration` interface
2. Implement the required `DowngradeAsync` method to reverse the changes made by the migration

### Example

```csharp
public class UserTableMigration : CodeMigration, IDowngradeMigration
{
    /// <inheritdoc />
    public override MigrationVersion Version { get; } = new(1, 0);
    
    /// <inheritdoc />
    public override string Comment => "Adds user table";

    /// <inheritdoc />
    public override Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // Create user table
        return MigrationConnection.ExecuteNonQuerySqlAsync(
            "CREATE TABLE Users (Id INT PRIMARY KEY, Name VARCHAR(100), Email VARCHAR(255))",
            transaction,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DowngradeAsync(DbTransaction? transaction = null, CancellationToken token = default)
    {
        // Drop user table to revert the migration
        return MigrationConnection.ExecuteNonQuerySqlAsync(
            "DROP TABLE Users",
            transaction,
            token);
    }
}
```

When implementing the `DowngradeAsync` method, ensure it properly reverts all changes made by the `UpgradeAsync` method. Take care to handle data that may be lost during downgrade operations.