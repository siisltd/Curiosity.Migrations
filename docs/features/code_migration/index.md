# Code Migration


Code migrations are particularly useful when you need more flexibility and power than what plain SQL scripts can provide. Consider using code migrations in the following scenarios.


## When to Use Code Migrations

### Complex Data Transformations

When you need to perform complex data transformations, calculations, or business logic that would be difficult or impossible to express in SQL alone. For example:

- Applying conditional data updates based on multiple criteria
- Performing complex calculations on data before updating
- Converting data between different formats or structures
- Implementing complex validation rules during migration

### Large Dataset Processing

When working with large datasets that would cause performance issues if processed in a single SQL transaction:

- Breaking down large updates into manageable batches
- Implementing pause logic between update batches to reduce database load
- Using the `MassUpdateCodeMigrationBase` to efficiently process large tables
- Monitoring progress of long-running operations

### External System Integration

When your migration needs to interact with systems outside the database:

- Calling external APIs or web services
- Reading from or writing to files
- Integrating with message queues or event systems
- Performing migrations that span multiple databases or data stores

### Advanced Database Operations

For operations that require more control than standard SQL scripts:

- Executing operations that must run outside a transaction (like certain index creations)
- Dynamically generating SQL based on existing database state
- Implementing retry logic for specific operations
- Performing database operations with different credentials or connection settings

### Dependency Injection

When you need to leverage your application's dependency injection framework:

- Reusing existing service components in your migrations
- Accessing configuration services or settings
- Maintaining consistency with application business logic
- Utilizing logging or monitoring services

### Environment-Specific Logic

When migration behavior needs to vary based on environment:

- Conditional execution based on environment variables
- Dynamic configuration based on deployment target
- Different data handling for development vs. production

In general, choose code migrations when you need the full power and flexibility of C# combined with database operations, and use script migrations for straightforward schema changes and simple data manipulations.

## How to Implement

To implement a code migration, you need to create a class that inherits from the `CodeMigration` abstract class and override the required members.

### Basic Implementation

Here's a basic example of implementing a code migration:

```csharp
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Curiosity.Migrations;

public class AddUsersTableMigration : CodeMigration
{
    // Define the migration version - required
    public override MigrationVersion Version => new MigrationVersion(1, 0);

    // Provide a descriptive comment - required
    public override string? Comment => "Add Users table";

    // Implement the upgrade logic - required
    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = @"CREATE TABLE Users (
            Id INT PRIMARY KEY,
            Username VARCHAR(100) NOT NULL,
            Email VARCHAR(100) NOT NULL
        )";

        await MigrationConnection.ExecuteNonQuerySqlAsync(sql, null, cancellationToken);
    }
}
```

### Configuring Migration Properties

Code migrations allow you to configure various properties:

```csharp
public class ComplexMigration : CodeMigration
{
    public override MigrationVersion Version => new MigrationVersion(2, 0);
    public override string? Comment => "Complex migration with custom properties";

    public ComplexMigration()
    {
        // Disable transaction (useful for operations like VACUUM or CREATE INDEX CONCURRENTLY)
        IsTransactionRequired = false;
        
        // Mark as long-running to help the migration engine manage resources
        IsLongRunning = true;
        
        // Define dependencies (migrations that must be applied before this one)
        Dependencies = new List<MigrationVersion>() 
        { 
            new(1, 0) 
        };
    }

    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // Your migration implementation here
    }
}
```

### Registering Code Migrations

To use your code migrations, you need to register them with the migration engine:

```csharp
var services = new ServiceCollection();
// Register your services for dependency injection
services.AddTransient<IDataService, DataService>();

// Build the migration engine
var builder = new MigrationEngineBuilder(services)
    .UseCodeMigrations()
    .FromAssembly(typeof(AddUsersTableMigration).Assembly) // Register all migrations in the assembly
    .ConfigureForPostgreSql("YourConnectionString");

// Build and run the migration engine
var migrationEngine = builder.Build();
await migrationEngine.UpgradeDatabaseAsync();
```

## Executing SQL Commands in Code Migrations

Code migrations allow you to execute SQL commands directly within your C# code. The `IMigrationConnection` interface provides several methods for executing SQL commands:

### Basic SQL Execution Methods

- **ExecuteNonQuerySqlAsync** - Executes a SQL script with DDL or DML commands
- **ExecuteScalarSqlAsync** - Executes a SQL script and returns a scalar value
- **ExecuteNonQuerySqlWithoutInitialCatalogAsync** - Executes a SQL script on the default database
- **ExecuteScalarSqlWithoutInitialCatalogAsync** - Executes a SQL query with returned value on the default database

### Executing SQL Commands with Parameters

The SQL execution methods accept an optional `queryParams` parameter, allowing you to use parameterized queries to prevent SQL injection:

```csharp
public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
{
    var sql = @"CREATE TABLE Users (
        Id INT PRIMARY KEY,
        Username VARCHAR(100) NOT NULL,
        Email VARCHAR(100) NOT NULL
    )";

    await MigrationConnection.ExecuteNonQuerySqlAsync(sql, null, cancellationToken);
    
    // Insert with parameters
    var insertSql = @"INSERT INTO Users (Id, Username, Email) 
                      VALUES (@id, @username, @email)";
    
    var parameters = new Dictionary<string, object?>
    {
        {"@id", 1},
        {"@username", "admin"},
        {"@email", "admin@example.com"}
    };
    
    await MigrationConnection.ExecuteNonQuerySqlAsync(insertSql, parameters, cancellationToken);
}
```

### Retrieving Data with SQL Queries

To execute a query that returns a scalar value:

```csharp
public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
{
    var countSql = "SELECT COUNT(*) FROM Users";
    var count = await MigrationConnection.ExecuteScalarSqlAsync(countSql, null, cancellationToken);
    
    // Use the result
    Logger?.LogInformation($"User count: {count}");
}
```

### SQL Command Execution Without Database Context

In some cases, you may need to execute commands without specifying the initial catalog (database name):

```csharp
public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
{
    // Check if a database exists
    var sql = "SELECT 1 FROM sys.databases WHERE name = @dbName";
    var parameters = new Dictionary<string, object?>
    {
        {"@dbName", "YourDatabaseName"}
    };
    
    var result = await MigrationConnection.ExecuteScalarSqlWithoutInitialCatalogAsync(
        sql, parameters, cancellationToken);
    
    if (result == null)
    {
        Logger?.LogInformation("Database does not exist, creating it...");
        // Additional logic for database creation
    }
}
```

### Long-Running SQL Operations

For operations that may take a long time, such as mass data updates, you can use the `MassUpdateCodeMigrationBase` class:

```csharp
public class UpdateUserDataMigration : MassUpdateCodeMigrationBase
{
    public override MigrationVersion Version => new MigrationVersion(1, 2);
    public override string? Comment => "Update user data in batches";
    
    public UpdateUserDataMigration() : base(TimeSpan.FromMilliseconds(100))
    {
        // Transaction is managed internally by base class
    }
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var updateQuery = @"
        WITH cte AS (
            SELECT id
            FROM Users 
            WHERE id > @id
            ORDER BY id
            LIMIT 1000)
        UPDATE Users u
            SET status = 'active'
        FROM cte
        WHERE cte.id = u.id
        RETURNING cte.id;";
        
        var totalUpdated = await DoMassUpdateAsync(
            updateQuery,
            (stepCount, totalCount) => 
            {
                Logger?.LogInformation($"Updated {stepCount} records (total: {totalCount})");
            },
            cancellationToken);
        
        Logger?.LogInformation($"Total records updated: {totalUpdated}");
    }
}
```

This approach allows you to process large datasets in batches, minimizing database load and preventing long-running transactions.