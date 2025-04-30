# Basics

This article explains the fundamental concepts of `Curiosity.Migrations` that you need to understand before diving into specific features.

## How the Migration Engine Works

The `Curiosity.Migrations` library follows a structured process to manage database schema and data changes safely and consistently:

```
┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
│  1. Configuration │     │  2. Infrastructure │     │  3. Version       │
│     Setup         │────▶│     Preparation    │────▶│     Comparison    │
└───────────────────┘     └───────────────────┘     └───────────────────┘
                                                              │
                                                              ▼
┌───────────────────┐     ┌───────────────────┐     ┌───────────────────┐
│  6. Result        │     │  5. Migration     │     │  4. Migration     │
│     Handling      │◀────│     Execution     │◀────│     Planning      │
└───────────────────┘     └───────────────────┘     └───────────────────┘
```

### 1. Configuration

The migration process begins with configuring the `MigrationEngine` using the `MigrationEngineBuilder`. This involves:

- Setting up migration providers (script migrations, code migrations)
- Configuring database connection details
- Defining migration policies for upgrades and downgrades
- Setting the target version (if needed)
- Configuring logging and error handling

```csharp
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("./Migrations")
    .ConfigureForPostgreSql("Host=localhost;Database=mydb;Username=postgres;Password=password")
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
```

### 2. Infrastructure Preparation

The `MigrationEngine` ensures the necessary infrastructure exists:

- **Database**: If the database doesn't exist, it's created according to connection settings
- **Migration Journal**: A table is created or verified to track applied migrations

### 3. Version Comparison

The migrator determines what needs to be done by comparing:

- Currently applied migrations (from the journal table)
- Available migrations (from providers)
- Target version (specified in configuration)

### 4. Migration Planning

Based on the comparison:

- For **upgrades**: Plans to apply new migrations in ascending version order
- For **downgrades**: Plans to apply downgrade migrations in descending version order
- Filters migrations according to the configured policy (e.g., only short-running)

### 5. Migration Execution

Migrations are executed according to the plan:

- Transactions are handled based on migration configuration
- Pre-migrations are executed first (if configured)
- Results and progress are logged
- Journal table is updated as migrations are applied

### 6. Result Handling

The process concludes by:

- Returning detailed results of the migration operation
- Logging completion status
- Handling any errors that occurred during migration

## Versioning System

`Curiosity.Migrations` provides a flexible versioning system to organize and sequence your database changes.

### Version Structure

A migration version consists of:

- **Major**: Required primary version number (e.g., `1`, `20230101`)
- **Minor**: Optional secondary version number after a dot (e.g., `.1`, `.42`)

The complete pattern recognized is: `([\d\_]+)(\.(\d+))*`

### Version Examples

Valid version formats include:

| Version Format | Example | Comments |
|----------------|---------|----------|
| Simple number | `1` | Basic sequential numbering |
| Decimal number | `1.5` | Major.Minor versioning |
| Date-based | `20230101` | Using date as version (YYYYMMDD) |
| Timestamp | `20230101_1430` | Date with time (YYYYMMDD_HHMM) |
| Complex | `20230101_1430.5` | Timestamp with minor version |

### Best Practices for Versioning

1. **Consistency**: Choose one versioning scheme and stick with it
2. **Simplicity**: For small projects, simple numbers (`1`, `2`, `3`) are often sufficient
3. **Timestamp-based**: For larger teams, date-based versions (`YYYYMMDD`) help avoid conflicts, especialy on mergre requests
4. **Minor versions**: Use minor versions to group related migrations that must run in sequence

```csharp
// Examples of creating version objects
var simpleVersion = new MigrationVersion(1);
var decimalVersion = new MigrationVersion(1, 5);
var dateVersion = new MigrationVersion(20230101);
```

## Target Version Management

The target version controls which migrations should be applied or rolled back.

### Setting a Target Version

```csharp
// Set a specific target version (migrate to exactly version 3)
builder.SetUpTargetVersion(new MigrationVersion(3));

// Migrate to the latest available version (default behavior)
// No need to call SetUpTargetVersion
```

### Migration Direction

The migration direction is not determined automatically. You need to manualy specify what you want - upgrade or downgrade.

Example:
```csharp
// Configure and build the migration engine
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("path/to/migrations")
    .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly())
    .ConfigureForPostgreSql("YourConnectionString")
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed)
    .UseDowngradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed);

// For upgrading to the latest version
var migrationEngine = builder.Build();
await migrationEngine.UpgradeDatabaseAsync();

// For upgrading to a specific version
builder.SetUpTargetVersion(new MigrationVersion(3));
var migrationEngine = builder.Build();
await migrationEngine.UpgradeDatabaseAsync();

// For downgrading to a specific version
builder.SetUpTargetVersion(new MigrationVersion(1));
var migrationEngine = builder.Build();
await migrationEngine.DowngradeDatabaseAsync();
```

## Migration Types: Short-Running vs Long-Running

`Curiosity.Migrations` categorizes migrations as either short-running or long-running to help manage resource utilization and scheduling.

### Key Differences

| Short-Running Migrations | Long-Running Migrations |
|--------------------------|-------------------------|
| Schema changes, small data updates | Large data transformations |
| Typically completes in seconds | May run for minutes or hours (even days)|
| Safe to run during application startup | Better scheduled during maintenance windows |
| Default for all migrations | Must be explicitly configured |

### Use Cases

**Short-Running Example**: Adding a new column to a table
```sql
-- CURIOSITY: LONG-RUNNING = FALSE
ALTER TABLE users ADD COLUMN email VARCHAR(255);
```

**Long-Running Example**: Populating data for millions of rows
```sql
-- CURIOSITY: LONG-RUNNING = TRUE
UPDATE users SET email = CONCAT(username, '@example.com');
```

### Configuring Migration Types

**For code migrations**:
```csharp
public class PopulateUserEmails : CodeMigration
{
    public override MigrationVersion Version => new MigrationVersion(1, 1);
    public override string? Comment => "Populate user email addresses";
    
    public PopulateUserEmails()
    {
        IsLongRunning = true; // Mark as long-running
    }
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

**For script migrations**:
Add a special comment at the top of your SQL file:
```sql
-- CURIOSITY: LONG-RUNNING = TRUE
```

## Migration Policies

Migration policies control which types of migrations are allowed to run in different scenarios.

### Available Policies

- **AllForbidden** (0): No migrations are allowed to run
- **ShortRunningAllowed** (1): Only short-running migrations can run
- **LongRunningAllowed** (2): Only long-running migrations can run
- **AllAllowed** (3): All migrations can run, regardless of type

### Configuring Policies

You can set different policies for upgrade and downgrade operations:

```csharp
var builder = new MigrationEngineBuilder(services)
    // Allow all migrations during upgrades
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed)
    // Only allow short-running migrations during downgrades
    .UseDowngradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed);
```

### Policy Usage Scenarios

1. **Development Environment**:
   ```csharp
   .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed)
   .UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed)
   ```

2. **Production Application Startup**:
   ```csharp
   .UseUpgradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed)
   .UseDowngradeMigrationPolicy(MigrationPolicy.AllForbidden)
   ```

3. **Scheduled Maintenance Window**:
   ```csharp
   .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed)
   .UseDowngradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed)
   ```

## Migration Providers

Migration providers determine where migrations come from. You can configure multiple providers to source migrations from different locations.

```csharp
var builder = new MigrationEngineBuilder(services)
    // Add migrations from SQL scripts
    .UseScriptMigrations().FromDirectory("./Migrations/Scripts")
    // Add migrations from embedded resources
    .UseScriptMigrations().FromEmbeddedResources(Assembly.GetExecutingAssembly(), "MyNamespace.Migrations")
    // Add migrations from code
    .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly());
```

For more information on available providers and custom implementations, see the [Migration Providers](./features/migration_providers.md) article.

## Complete Configuration Example

Here's a complete example showing how to configure the migration engine with all major options:

```csharp
using Curiosity.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Create service collection
var services = new ServiceCollection();
services.AddLogging(configure => configure.AddConsole());

// Configure and build the migration engine
var builder = new MigrationEngineBuilder(services)
    // Add script migrations from directory
    .UseScriptMigrations()
        .FromDirectory("./DatabaseMigrations")
        .WithScriptIncorrectNamingAction(ScriptIncorrectNamingAction.ThrowException)
    // Add code migrations from assembly
    .UseCodeMigrations()
        .FromAssembly(typeof(Program).Assembly)
    // Configure database connection
    .ConfigureForPostgreSql("Host=localhost;Database=myapp;Username=postgres;Password=secret")
    // Set migration policies
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed)
    .UseDowngradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed)
    // Add variables for substitution in scripts
    .UseVariable("%SCHEMA%", "public")
    .UseVariable("%TABLE_PREFIX%", "app_")
    // Configure journal table
    .UseJournalTable("migration_history")
    // Configure pre-migrations
    .UsePreMigrations()
        .AddPreMigrationScript("CREATE EXTENSION IF NOT EXISTS pg_trgm;")
    // Set target version (optional)
    .SetUpTargetVersion(new MigrationVersion(20230101));

// Build the engine
var migrationEngine = builder.Build();

// Execute migrations
var result = await migrationEngine.UpgradeDatabaseAsync();

// Handle results
if (result.IsSuccessful)
{
    Console.WriteLine($"Successfully migrated to version {result.CurrentVersion}");
    Console.WriteLine($"Applied {result.AppliedMigrations.Count} migrations");
}
else
{
    Console.WriteLine($"Migration failed: {result.ErrorMessage}");
}
```

## Troubleshooting Common Issues

### Version Parsing Problems

**Problem**: Migration version cannot be parsed from filename or class.

**Solution**: Ensure your version format matches the pattern `([\d\_]+)(\.(\d+))*`. 
Check for common mistakes like using letters in version numbers.

```csharp
// Correct
new MigrationVersion(1, 5);          // 1.5
new MigrationVersion(20230101);      // 20230101

// Incorrect (will cause errors)
// new MigrationVersion("v1");       // Cannot use letters
// new MigrationVersion(-1);         // Cannot use negative numbers
```

### Missing Dependencies

**Problem**: Migrations fail with errors about missing dependencies.

**Solution**: Ensure all referenced migrations exist and are available to the migration engine.
Check that your dependency versions are correctly specified.

### Transaction Errors

**Problem**: Migrations fail with transaction-related errors.

**Solution**: Some operations cannot run within a transaction (like certain DDL statements in some databases).
Set `IsTransactionRequired = false` for these migrations:

```csharp
public class CreateIndexMigration : CodeMigration
{
    public CreateIndexMigration()
    {
        IsTransactionRequired = false; // Disable transaction for this migration
    }
    
    // Implementation
}
```

### Database Connection Issues

**Problem**: Migrations fail with connection errors.

**Solution**: 
- Verify connection string parameters
- Ensure the database server is running and accessible
- Check network connectivity and firewall settings
- Verify the user has sufficient permissions

### Policy Restrictions

**Problem**: Migrations don't run due to policy restrictions.

**Solution**: Check your migration policy settings and ensure they allow the types of migrations you're trying to run:

```csharp
// Make sure your policy allows the migrations you want to run
builder.UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
```

For more detailed information on specific features, please refer to the corresponding feature articles.
