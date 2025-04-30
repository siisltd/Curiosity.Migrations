# Downgrade Migrations

## Overview

Downgrade migrations are a powerful feature in Curiosity.Migrations that allow you to revert your database to a previous state. While most migration systems focus primarily on forward evolution, Curiosity.Migrations gives equal importance to the ability to safely roll back changes when needed.

This bidirectional migration capability provides safety nets for production deployments, flexibility during development, and easier management of complex release strategies.

## When to Use Downgrade Migrations

Downgrade migrations serve several important purposes:

1. **Production Rollbacks**: When a deployment introduces issues, quickly revert to the last stable version
2. **Development Flexibility**: Move back and forth between database versions during development
3. **Feature Branching**: Support multiple development branches with different database schemas
4. **A/B Testing**: Enable database schema differences for testing alternative implementations
5. **Phased Deployments**: Implement gradual rollouts with the ability to retreat if needed
6. **Release Management**: Coordinate database changes with application deployments
7. **Disaster Recovery**: Have a well-tested path back to a known-good state

## How Downgrade Migrations Work

When you execute a downgrade operation:

1. The migration engine identifies which migrations need to be rolled back (those with versions higher than the target)
2. It sorts these migrations in descending order (newest to oldest)
3. For each migration, it executes the corresponding downgrade logic
4. After successfully downgrading each migration, it removes the entry from the migration journal

This ensures the database returns to the desired previous state in an orderly manner.

## Implementation Options

Curiosity.Migrations offers two ways to implement downgrade migrations:

### 1. Script-Based Downgrades

For SQL script migrations, create matching `.up.sql` and `.down.sql` files:

- **Naming Convention**: 
  - Upgrade: `<version>.up.sql` or `<version>.sql`
  - Downgrade: `<version>.down.sql`

- **Example File Structure**:
  ```
  migrations/
  ├── 1.0.up.sql     # Create users table
  ├── 1.0.down.sql   # Drop users table 
  ├── 2.0.up.sql     # Add email column
  └── 2.0.down.sql   # Remove email column
  ```

### 2. Code-Based Downgrades

For code migrations, implement the `IDowngradeMigration` interface:

```csharp
public class AddEmailToUsersMigration : CodeMigration, IDowngradeMigration
{
    public override MigrationVersion Version => new(2, 0);
    
    public override string? Comment => "Add email column to Users table";
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync(
            "ALTER TABLE Users ADD Email VARCHAR(255);",
            null,
            cancellationToken);
    }
    
    public async Task DowngradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync(
            "ALTER TABLE Users DROP COLUMN Email;",
            null,
            cancellationToken);
    }
}
```

## Detailed Implementation Examples

### Script Migration Example

**1.0.up.sql** - Create Users table:
```sql
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(100) NOT NULL UNIQUE,
    FullName VARCHAR(200) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Add an index for username lookups
CREATE INDEX idx_users_username ON Users(Username);

-- Add a comment on the table
COMMENT ON TABLE Users IS 'Stores user account information';
```

**1.0.down.sql** - Drop Users table:
```sql
-- Drop the table and all associated objects
DROP TABLE IF EXISTS Users CASCADE;
```

**2.0.up.sql** - Add email to Users:
```sql
-- Add email column
ALTER TABLE Users ADD COLUMN Email VARCHAR(255);

-- Add unique constraint
ALTER TABLE Users ADD CONSTRAINT uq_users_email UNIQUE (Email);

-- Add index for email searches
CREATE INDEX idx_users_email ON Users(Email);
```

**2.0.down.sql** - Remove email:
```sql
-- Remove all objects related to email column
DROP INDEX IF EXISTS idx_users_email;
ALTER TABLE Users DROP CONSTRAINT IF EXISTS uq_users_email;
ALTER TABLE Users DROP COLUMN IF EXISTS Email;
```

### Code Migration Example with Data Preservation

This example shows a more complex downgrade scenario with data preservation:

```csharp
public class SplitNameFieldsMigration : CodeMigration, IDowngradeMigration
{
    public override MigrationVersion Version => new(3, 0);
    
    public override string? Comment => "Split FullName into FirstName and LastName";
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        // Add new columns
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users ADD FirstName VARCHAR(100);
            ALTER TABLE Users ADD LastName VARCHAR(100);
        ", transaction, cancellationToken);
        
        // Split existing data
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            UPDATE Users 
            SET 
                FirstName = SPLIT_PART(FullName, ' ', 1),
                LastName = SUBSTRING(FullName FROM POSITION(' ' IN FullName) + 1)
            WHERE 
                FullName IS NOT NULL AND FullName != '';
        ", transaction, cancellationToken);
        
        // Make the new columns non-nullable
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users ALTER COLUMN FirstName SET NOT NULL;
            ALTER TABLE Users ALTER COLUMN LastName SET NOT NULL;
        ", transaction, cancellationToken);
        
        // Drop the original column
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users DROP COLUMN FullName;
        ", transaction, cancellationToken);
    }
    
    public async Task DowngradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        // Recreate the original column
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users ADD FullName VARCHAR(200);
        ", transaction, cancellationToken);
        
        // Combine the split data
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            UPDATE Users 
            SET FullName = CONCAT(FirstName, ' ', LastName)
            WHERE FirstName IS NOT NULL OR LastName IS NOT NULL;
        ", transaction, cancellationToken);
        
        // Make the original column non-nullable
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users ALTER COLUMN FullName SET NOT NULL;
        ", transaction, cancellationToken);
        
        // Drop the split columns
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users DROP COLUMN FirstName;
            ALTER TABLE Users DROP COLUMN LastName;
        ", transaction, cancellationToken);
    }
}
```

## How to Execute a Downgrade

To downgrade your database to a specific version:

1. **Configure the Migration Engine**:

```csharp
var builder = new MigrationEngineBuilder(services)
    // Add migration sources
    .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly())
    .UseScriptMigrations().FromDirectory("./Migrations")
    
    // Configure database connection
    .ConfigureForPostgreSql("YourConnectionString")
    
    // Set downgrade policy
    .UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed)
    
    // Specify target version to downgrade to
    .SetUpTargetVersion(new MigrationVersion(1, 5));

var migrationEngine = builder.Build();
```

2. **Execute the Downgrade**:

```csharp
var result = await migrationEngine.DowngradeDatabaseAsync();

if (result.IsSuccessful)
{
    Console.WriteLine($"Successfully downgraded from {result.PreviousVersion} to {result.CurrentVersion}");
    Console.WriteLine($"Rolled back {result.AppliedMigrations.Count} migrations");
}
else
{
    Console.WriteLine($"Downgrade failed: {result.ErrorMessage}");
}
```

## Managing Downgrade Risks

Downgrade migrations inherently carry risks, especially regarding data preservation. Here are strategies to mitigate these risks:

### Data Loss Risks

The following operations require special care during downgrades:

| Operation | Risk | Mitigation Strategy |
|-----------|------|---------------------|
| Dropping columns | Data in those columns is lost | Preserve data in temporary tables or JSON fields |
| Changing column types | Data might be truncated | Add validation to prevent data loss |
| Schema redesign | Complex structural changes | Implement multi-step migrations with intermediate states |
| Removing tables | All table data is lost | Archive data before dropping tables |
| Adding constraints | Existing data might violate constraints | Add data cleaning step before constraints |

### Best Practices for Safe Downgrades

1. **Test Thoroughly**: Always test downgrade migrations in development and staging environments before using them in production.

2. **Backup Before Downgrading**: Create a database backup before executing any downgrade operation in production.

> You can make backups with pre-migration feature of Curiosity.Migrations.  

3. **Version in Small Increments**: Smaller, more focused migrations are easier to downgrade reliably.

4. **Data Preservation Patterns**: Implement patterns that preserve data during downgrade operations:
   - Use temporary tables to store data that would otherwise be lost
   - Consider JSON columns for storing data during schema transitions
   - Use staging tables for complex structural changes

5. **Validation Checks**: Add validation to both upgrade and downgrade migrations to ensure data integrity.

6. **Transactional Safety**: Ensure migrations use transactions appropriately to prevent partial application.

## Pattern: Safe Column Removal with Data Preservation

Here's a pattern for safely removing columns with the ability to restore data during downgrades:

**Upgrade**:
1. Add a new column or structure
2. Migrate data from old column to new structure
3. Remove old column (after verification)

**Downgrade**:
1. Recreate the old column
2. Migrate data back from new structure to old column
3. Remove new structure (after verification)

Example:

```csharp
public class SafeColumnRemovalMigration : CodeMigration, IDowngradeMigration
{
    public override MigrationVersion Version => new(4, 0);
    
    public override string? Comment => "Replace UserSettings column with UserSettings table";
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        // 1. Create new structure
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            CREATE TABLE UserSettings (
                UserId INT NOT NULL REFERENCES Users(Id),
                SettingKey VARCHAR(100) NOT NULL,
                SettingValue TEXT,
                PRIMARY KEY (UserId, SettingKey)
            );
        ", transaction, cancellationToken);
        
        // 2. Migrate data
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            INSERT INTO UserSettings (UserId, SettingKey, SettingValue)
            SELECT 
                Id as UserId,
                json_object_keys(Settings::json) as SettingKey,
                Settings::json->>json_object_keys(Settings::json) as SettingValue
            FROM 
                Users
            WHERE 
                Settings IS NOT NULL AND Settings != '';
        ", transaction, cancellationToken);
        
        // 3. Remove old column
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users DROP COLUMN Settings;
        ", transaction, cancellationToken);
    }
    
    public async Task DowngradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        // 1. Recreate old column
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            ALTER TABLE Users ADD COLUMN Settings JSONB;
        ", transaction, cancellationToken);
        
        // 2. Migrate data back
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            UPDATE Users u
            SET Settings = s.settings_json
            FROM (
                SELECT 
                    UserId, 
                    jsonb_object_agg(SettingKey, SettingValue) as settings_json
                FROM 
                    UserSettings
                GROUP BY 
                    UserId
            ) s
            WHERE u.Id = s.UserId;
        ", transaction, cancellationToken);
        
        // 3. Remove new structure
        await MigrationConnection.ExecuteNonQuerySqlAsync(@"
            DROP TABLE UserSettings;
        ", transaction, cancellationToken);
    }
}
```

## Summary

Downgrade migrations provide a safety net that allows you to move your database schema backward when necessary. By carefully planning both upgrade and downgrade paths, you can create a more robust migration system that handles the full lifecycle of your database schema.

Remember that downgrade migrations require special attention to data preservation and integrity. Always test your downgrade migrations thoroughly before relying on them in production environments.