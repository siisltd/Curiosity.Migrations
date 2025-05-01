# Migration Dependencies

## Overview

Migration dependencies are a powerful feature in `Curiosity.Migrations` that allow you to explicitly define relationships between migrations. Rather than relying solely on version numbers to determine execution order, dependencies give you precise control over which migrations must be applied before others.

This feature is essential for managing complex migration scenarios, particularly in large applications with multiple development teams or when migrations affect related database objects but are developed independently.

## When to Use Dependencies

Dependencies are especially valuable in these scenarios:

1. **Cross-Module Migrations**: When migrations in different modules or components need to be coordinated
2. **Logical Grouping**: When related migrations should be applied together despite having non-sequential versions
3. **Complex Database Objects**: When certain database objects must exist before others can be created
4. **Team Collaboration**: When multiple teams are contributing migrations that may have interdependencies
5. **Hotfixes**: When a critical fix needs to build upon existing migrations but use a special version number
6. **Complex migration**: When running firstly short running migration (like add column), then running long running migration to fill it (like update set for million of rows), and lastly running short running migration to make column nullable

## How Dependencies Work

When you specify dependencies for a migration:

1. The migration engine validates that all specified dependencies exist in the available migrations
2. Before applying the migration, it verifies that all dependencies have been successfully applied
3. If any dependency is missing or hasn't been applied, the migration fails with a `MigrationErrorCode.MigratingError`

Dependencies work alongside the standard version-based ordering. The engine still applies migrations in version order, but adds the additional constraint that all dependencies must be satisfied before a migration runs.

## Implementing Dependencies in Script Migrations

For SQL script migrations, specify dependencies using the `--CURIOSITY:Dependencies` directive at the beginning of your file:

```sql
-- Version: 3.0
-- Migration to add user permissions
--CURIOSITY:Dependencies=1.0, 2.0

CREATE TABLE user_permissions (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id),
    permission_name VARCHAR(100) NOT NULL,
    granted_at TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_user_permissions_user_id ON user_permissions(user_id);
```

In this example, the migration declares that versions 1.0 and 2.0 must be applied before it can run. This might be because those migrations create the "users" table and add necessary columns that this migration references.


The `--CURIOSITY:Dependencies` directive should:

- Appear at the beginning of the file (within the first few lines)
- List dependencies as comma-separated version numbers
- Use the same version format as your migration versioning scheme

Examples:

```sql
--CURIOSITY:Dependencies=1.0, 2.0                  -- Simple versions
--CURIOSITY:Dependencies=20230101, 20230102.1      -- Date-based versions
--CURIOSITY:Dependencies=2023_01_01, 2023_01_02    -- Versions with underscores
```

## Implementing Dependencies in Code Migrations

For code migrations, set the `Dependencies` property in your migration class:

```csharp
public class AddUserSettingsMigration : CodeMigration
{
    public override MigrationVersion Version => new(3, 0);
    
    public override string? Comment => "Add user settings table";

    // Set dependencies in the constructor
    public AddUserSettingsMigration()
    {
        // This migration depends on versions 1.0 and 2.0
        Dependencies = new List<MigrationVersion>() 
        { 
            new(1, 0),  // Users table migration
            new(2, 0)   // User profile migration
        };
    }
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            CREATE TABLE user_settings (
                user_id INT NOT NULL REFERENCES users(id),
                setting_key VARCHAR(100) NOT NULL,
                setting_value TEXT,
                PRIMARY KEY (user_id, setting_key)
            );";
            
        await MigrationConnection.ExecuteNonQuerySqlAsync(
            sql, null, cancellationToken);
    }
}
```

This approach provides type safety and IntelliSense support for specifying dependencies.

## Troubleshooting

### Missing Dependency Errors

If you receive a `MigrationErrorCode.MigratingError` with a message about missing dependencies:

1. Verify that all dependent migrations exist in your migration source (directory, assembly, etc.)
2. Check that dependent migration versions are correctly specified
3. Ensure that dependent migrations have successfully applied to the database
4. Check the migration journal table to see the current state of applied migrations

### Circular Dependencies

Circular dependencies (A depends on B, and B depends on A) are not allowed and will cause errors during migration validation.

If you encounter circular dependency errors:

1. Redesign your migrations to eliminate the circular reference
2. Consider merging the migrations if they're tightly coupled
3. Create an intermediate migration that both can depend on

## Example: Migration Dependency Chain

Here's a complete example showing a chain of dependent migrations:

1. First, create the users table:

```sql
-- Version: 1.0
-- users.sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

2. Next, add authentication fields:

```sql
-- Version: 2.0
-- authentication.sql
--CURIOSITY:Dependencies=1.0

ALTER TABLE users 
ADD COLUMN password_hash VARCHAR(255) NOT NULL DEFAULT '',
ADD COLUMN email VARCHAR(255) UNIQUE,
ADD COLUMN last_login TIMESTAMP;
```

3. Then add permissions:

```sql
-- Version: 3.0
-- permissions.sql
--CURIOSITY:Dependencies=2.0

CREATE TABLE permissions (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT
);

CREATE TABLE user_permissions (
    user_id INT NOT NULL REFERENCES users(id),
    permission_id INT NOT NULL REFERENCES permissions(id),
    granted_at TIMESTAMP NOT NULL DEFAULT NOW(),
    PRIMARY KEY (user_id, permission_id)
);
```

4. Finally, seed initial permissions:

```csharp
public class SeedPermissionsMigration : CodeMigration
{
    public override MigrationVersion Version => new(3, 1);
    
    public override string? Comment => "Seed initial permissions";

    public SeedPermissionsMigration()
    {
        Dependencies = new List<MigrationVersion>() { new(3, 0) };
    }
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        var defaultPermissions = new[] {
            ("user.read", "Can view user information"),
            ("user.create", "Can create new users"),
            ("user.update", "Can modify user information"),
            ("user.delete", "Can delete users")
        };
        
        foreach (var (name, description) in defaultPermissions)
        {
            await MigrationConnection.ExecuteNonQuerySqlAsync(
                "INSERT INTO permissions (name, description) VALUES (@name, @description)",
                new Dictionary<string, object?> {
                    { "@name", name },
                    { "@description", description }
                },
                cancellationToken);
        }
    }
}
```

This chain of migrations ensures that:
- The users table exists before adding authentication fields
- Authentication fields exist before creating permission tables
- Permission tables exist before seeding permissions data