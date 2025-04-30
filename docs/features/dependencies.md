# Dependended Migrations

Migration dependencies allow you to specify which migrations must be applied before a particular migration can run. This is useful for:

- Ensuring a specific order of execution beyond simple version numbering
- Creating logical groupings of migrations that build on each other
- Preventing execution of migrations that rely on database objects created by other migrations
- Handling complex migration scenarios where certain migrations must exist before others can be applied

The migration engine validates these dependencies at runtime. If any dependency is missing, the migration will fail with a `MigrationErrorCode.MigratingError`.

## Script migrations

To specify dependencies for a script migration, use the `--CURIOSITY:Dependencies` directive at the beginning of your SQL script file. 

The directive should be placed at the top of the file and list the migration versions that must be applied before this migration can run:

```sql
--CURIOSITY:Dependencies=1.0, 2.0
```

This tells the migration engine that versions 1.0 and 2.0 must be successfully applied before this migration can be executed. Multiple dependencies are separated by commas.

The migration engine will validate these dependencies before attempting to apply the migration. If any dependency is missing, the migration will fail with a `MigrationErrorCode.MigratingError`.

## Code migrations

To specify dependencies for a code migration, set the `Dependencies` property in your migration class constructor:

```csharp
public class YourMigration : CodeMigration
{
    public override MigrationVersion Version { get; } = new(3);
    
    public override string Comment { get; } = "Migration with dependencies";

    public YourMigration()
    {
        Dependencies = new List<MigrationVersion>() 
        { 
            new(1, 0), 
            new(2, 0) 
        };
    }
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // Migration implementation
    }
}
```

This sets version 1.0 and 2.0 as dependencies for this migration. The migration engine will verify that these dependencies are applied before executing this migration. If any dependency is missing, the migration will fail with a `MigrationErrorCode.MigratingError`.