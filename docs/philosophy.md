# The Philosophy Behind Curiosity.Migrations

## Introduction

Curiosity.Migrations is a .NET database migration framework designed for enterprise-grade applications that require precise control, comprehensive safety features, and detailed monitoring when managing database schema and data changes. Unlike ORM-focused migration tools, Curiosity.Migrations embraces both direct SQL and C# code approaches to give developers maximum flexibility and performance control.

## Core Principles

### 1. Migration as Code

Curiosity.Migrations embraces the migration-as-code philosophy, treating database changes the same way you treat application code changes:

- **Version Control**: Every database change is versioned and tracked in your source repository
- **History Tracking**: Complete audit trail of all database modifications
- **Environment Consistency**: Same migration process across development, testing, and production
- **Schema-Code Alignment**: Database schema changes are synchronized with application code changes

```csharp
// Each migration is a discrete, versioned unit of change
public class AddEmailToUsers : CodeMigration
{
    public override MigrationVersion Version => new MigrationVersion(20230615);
    public override string? Comment => "Add email column to Users table";
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync(
            "ALTER TABLE users ADD COLUMN email VARCHAR(255);", 
            null, 
            cancellationToken);
    }
}
```

### 2. Raw SQL Migrations

The library prioritizes raw SQL migrations, giving developers precise control over database operations:

- **Performance Optimization**: Write highly optimized SQL for critical operations
- **Database-Specific Features**: Leverage database-specific features and syntax
- **Execution Transparency**: What you write is exactly what executes against your database
- **Full Control**: No "magic" or auto-generated queries with unexpected behavior

```sql
-- Version: 20230615
-- A SQL migration that adds an optimized index with database-specific options
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_users_email 
ON users(email) 
WHERE email IS NOT NULL;

-- Using database-specific functionality directly
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX IF NOT EXISTS idx_users_name_trigram 
ON users USING gin(name gin_trgm_ops);
```

### 3. Code Migrations

For complex data transformation scenarios, Curiosity.Migrations supports code-based migrations:

- **Complex Logic**: Implement sophisticated business rules during migration
- **External Integration**: Connect to external systems during migration process
- **Batched Processing**: Efficiently process large datasets with controlled resource usage
- **.NET Ecosystem**: Leverage the full power of C# and the .NET ecosystem

```csharp
public class NormalizeUserEmails : MassUpdateCodeMigrationBase
{
    public override MigrationVersion Version => new MigrationVersion(20230616);
    public override string? Comment => "Normalize email addresses to lowercase";
    
    // Process data in batches with a delay to reduce database load
    public NormalizeUserEmails() : base(TimeSpan.FromMilliseconds(100)) { }
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        // Complex processing with batching and external API integration
        await using var reader = await MigrationConnection.ExecuteReaderAsync(
            "SELECT id, email FROM users WHERE email IS NOT NULL;", 
            null, 
            cancellationToken);
            
        int batchSize = 1000;
        int processed = 0;
        
        while (await reader.ReadAsync(cancellationToken))
        {
            // Read data
            int id = reader.GetInt32(0);
            string email = reader.GetString(1);
            
            // Apply business logic
            string normalizedEmail = email.ToLowerInvariant();
            
            // Optionally validate with external email validation service
            // bool isValid = await emailValidationService.ValidateAsync(normalizedEmail);
            
            // Update data
            await MigrationConnection.ExecuteNonQuerySqlAsync(
                "UPDATE users SET email = @email WHERE id = @id;",
                new Dictionary<string, object?> { 
                    { "@id", id },
                    { "@email", normalizedEmail }
                },
                cancellationToken);
                
            // Process in batches with delays to reduce database load
            processed++;
            if (processed % batchSize == 0)
            {
                await Task.Delay(DelayBetweenBatches, cancellationToken);
                Logger?.LogInformation($"Processed {processed} records");
            }
        }
    }
}
```

### 4. Safety in Production

Curiosity.Migrations implements robust safety mechanisms for production environments:

- **Migration Policies**: Configure what types of migrations can run in different environments
- **Dependency Management**: Ensure migrations run in the correct order with explicit dependencies
- **Pre-execution Validation**: Verify database state before applying changes
- **Rollback Capabilities**: Ability to r evert failed migrations when issues occur
- **Long-running Migration Control**: Separate potentially dangerous long-running operations

```csharp
// Configure strict policies for production environments
var builder = new MigrationEngineBuilder(services)
    // Only allow safe, quick schema changes in production during startup
    .UseUpgradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed)
    // Prevent accidental downgrades in production
    .UseDowngradeMigrationPolicy(MigrationPolicy.AllForbidden);
```

### 5. Migration Progress Monitoring

The library provides built-in monitoring capabilities to track migration progress:

- **Detailed Logging**: Comprehensive logging of migration execution
- **Progress Reporting**: Real-time updates on long-running migration progress
- **Diagnostics**: Detailed information to help identify and resolve issues
- **Result Tracking**: Complete record of which migrations succeeded or failed

```csharp
// Configure logging for migrations
var builder = new MigrationEngineBuilder(services)
    .UseLogger(loggerFactory.CreateLogger<MigrationEngine>());
    
// Execute with detailed result information
var result = await migrationEngine.UpgradeDatabaseAsync();
if (result.IsSuccessful)
{
    logger.LogInformation(
        "Successfully migrated from {OldVersion} to {NewVersion}. " +
        "Applied {Count} migrations in {Time}ms.",
        result.PreviousVersion,
        result.CurrentVersion,
        result.AppliedMigrations.Count,
        result.ExecutionTime.TotalMilliseconds);
}
```

### 6. Testability

Curiosity.Migrations simplifies database testing:

- **Isolated Test Databases**: Easily create and initialize test databases
- **Migration State Control**: Test against specific database versions
- **Integration Testing**: Verify application compatibility across schema changes
- **Mock Support**: Test migration logic with mock database connections

```csharp
// In a test fixture
[SetUp]
public async Task SetUp()
{
    // Create test database with specific migrations applied
    var builder = new MigrationEngineBuilder(services)
        .UseScriptMigrations().FromDirectory("./TestMigrations")
        .ConfigureForPostgreSql(TestConnectionString)
        .SetUpTargetVersion(new MigrationVersion(20230501)); // Apply migrations up to this version
        
    var migrationEngine = builder.Build();
    await migrationEngine.UpgradeDatabaseAsync();
    
    // Now tests will run against a database at the specific version
}
```

## When to Use Curiosity.Migrations

Curiosity.Migrations is particularly well-suited for:

1. **Enterprise Applications**: Where safety, control, and reliability are paramount
2. **Performance-Critical Systems**: When you need optimized SQL for large datasets
3. **Complex Database Operations**: When migrations involve sophisticated business logic
4. **Multi-Environment Deployments**: When you need different behavior across dev/test/prod
5. **Long-Running Migrations**: When you need to manage migrations that affect millions of records

## Comparison to .NET Alternatives

Curiosity.Migrations offers distinct advantages when compared to other .NET database migration tools:

### Entity Framework Core Migrations

| Feature | EF Core Migrations | Curiosity.Migrations |
|---------|-------------------|----------------------|
| **Approach** | Code-first model-driven | SQL-first with code support |
| **SQL Control** | Generated from model changes | Direct, handcrafted SQL |
| **Performance** | May generate suboptimal SQL | Fully optimized, manual SQL |
| **Complex Logic** | Limited to model changes | Full C# capabilities |
| **Long-running Migration Support** | Basic | Comprehensive with batching |
| **Production Safety** | Basic | Advanced policy controls |
| **Best For** | Simple CRUD applications | Performance-critical systems |

### FluentMigrator

| Feature | FluentMigrator | Curiosity.Migrations |
|---------|----------------|----------------------|
| **Approach** | Fluent C# API | Direct SQL + C# code |
| **SQL Control** | Generated from fluent API | Direct SQL or generated |
| **Database Support** | Multiple databases | PostgreSQL (extensible) |
| **Complex Transformations** | Limited by API | Unrestricted with C# |
| **Monitoring** | Basic | Comprehensive |
| **Safety Features** | Basic | Advanced policy controls |
| **Best For** | Cross-database projects | Complex, database-specific needs |

### DbUp

| Feature | DbUp | Curiosity.Migrations |
|---------|------|----------------------|
| **Approach** | SQL script runner | SQL + code migrations |
| **Complexity** | Simple | More feature-rich |
| **Rollback Support** | Limited | Built-in |
| **Production Safeguards** | Basic | Comprehensive |
| **Monitoring** | Basic | Detailed progress tracking |
| **Code Migration Support** | Limited | First-class feature |
| **Best For** | Simple deployments | Enterprise applications |

### Evolve

| Feature | Evolve | Curiosity.Migrations |
|---------|--------|----------------------|
| **Approach** | Flyway-inspired | SQL + code migrations |
| **Testing Support** | Basic | Extensive |
| **Monitoring** | Basic | Comprehensive |
| **.NET Integration** | Good | Excellent |
| **Long-running Migration Support** | Limited | Extensive |
| **Best For** | Java developers familiar with Flyway | .NET-focused teams |

## Summary

Curiosity.Migrations balances the precision of direct SQL with the power of C# code migrations, while prioritizing safety, monitoring, and testability for enterprise-grade applications. It's designed for teams that need fine-grained control over their database changes and require robust processes for managing these changes across different environments.