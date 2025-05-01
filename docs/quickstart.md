# Quick Start Guide

## Introduction

`Curiosity.Migrations` is a flexible database migration framework for .NET applications that provides precise control over database schema and data changes. This quick start guide will help you get up and running with the basic functionality in just a few minutes.

By the end of this guide, you'll know how to:
- Install the necessary packages
- Configure the migration engine
- Create both SQL and code-based migrations
- Apply migrations to your database
- Integrate with dependency injection

## Installation

### 1. Install Required Packages

First, install the core package and the provider for your database system:

```bash
# Core package (required)
dotnet add package Curiosity.Migrations

# Database-specific package (choose one)
dotnet add package Curiosity.Migrations.PostgreSQL
# Future packages will include:
# dotnet add package Curiosity.Migrations.SqlServer
# dotnet add package Curiosity.Migrations.MySQL
```

### 2. Create Migration Directory Structure

For SQL script migrations, create a directory structure in your project:

```
YourProject/
├── Migrations/
│   ├── 1.0.sql        # First migration
│   ├── 2.0.sql        # Second migration
│   └── 3.0.sql        # Third migration
```

## Creating Your First Migrations

You can create migrations either as SQL scripts or as C# code. Let's see examples of both approaches.

### SQL Script Migrations

Create SQL files with version numbers in the filename. Here's an example of a script migration:

**Migrations/1.0.sql**:
```sql
-- Migration: Create Users table
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Username VARCHAR(100) NOT NULL UNIQUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Create index for username lookups
CREATE INDEX idx_users_username ON Users(Username);
```

**Migrations/2.0.sql**:
```sql
-- Migration: Add email to Users table
ALTER TABLE Users ADD COLUMN Email VARCHAR(255);
ALTER TABLE Users ADD CONSTRAINT uq_users_email UNIQUE (Email);
```

### Code Migrations

Create C# classes that inherit from `CodeMigration`. Here's an example:

```csharp
using Curiosity.Migrations;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace YourProject.Migrations
{
    public class AddProfileDataMigration : CodeMigration
    {
        // Specify the version - must be unique
        public override MigrationVersion Version => new(3, 0);

        // Optional description of what this migration does
        public override string? Comment => "Add profile data to Users table";

        // The migration implementation
        public override async Task UpgradeAsync(
            DbTransaction? transaction = null, 
            CancellationToken cancellationToken = default)
        {
            var sql = @"
                ALTER TABLE Users 
                ADD COLUMN FirstName VARCHAR(100),
                ADD COLUMN LastName VARCHAR(100),
                ADD COLUMN Bio TEXT;
                
                CREATE INDEX idx_users_name 
                ON Users(LastName, FirstName);
            ";

            await MigrationConnection.ExecuteNonQuerySqlAsync(
                sql, null, cancellationToken);
        }
    }
}
```

## Configuring and Running Migrations

### Basic Configuration and Execution

Here's how to configure the migration engine and run migrations:

```csharp
using Curiosity.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace YourProject
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Create service collection
            var services = new ServiceCollection();
            services.AddLogging();

            // 2. Configure migration engine
            var builder = new MigrationEngineBuilder(services)
                // Add SQL script migrations
                .UseScriptMigrations()
                    .FromDirectory("./Migrations")
                // Add code migrations
                .UseCodeMigrations()
                    .FromAssembly(Assembly.GetExecutingAssembly())
                // Configure database connection
                .ConfigureForPostgreSql("Host=localhost;Database=myapp;Username=postgres;Password=secret")
                // Set migration policies
                .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);

            // 3. Build the migration engine
            var migrationEngine = builder.Build();

            try
            {
                // 4. Run migrations
                var result = await migrationEngine.UpgradeDatabaseAsync();

                // 5. Report results
                if (result.IsSuccessful)
                {
                    Console.WriteLine($"Applied {result.AppliedMigrations.Count} migrations");
                }
                else
                {
                    Console.WriteLine($"Migration failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running migrations: {ex.Message}");
            }
        }
    }
}
```

### ASP.NET Core Integration

In an ASP.NET Core application, you can add migrations to your `Startup.cs` or `Program.cs`:

```csharp
// Program.cs in .NET 6+
var builder = WebApplication.CreateBuilder(args);

// Add migrations to the service collection
builder.Services.AddMigrations(migrationsBuilder =>
{
    migrationsBuilder
        .UseScriptMigrations().FromDirectory("./Migrations")
        .UseCodeMigrations().FromAssembly(typeof(Program).Assembly)
        .ConfigureForPostgreSql(builder.Configuration.GetConnectionString("DefaultConnection"))
        .UseUpgradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed);
});

// Add a hosted service to run migrations on startup
builder.Services.AddHostedService<MigrationHostedService>();

var app = builder.Build();
// Configure app...
app.Run();

// MigrationHostedService.cs
public class MigrationHostedService : IHostedService
{
    private readonly IMigrationEngine _migrationEngine;
    private readonly ILogger<MigrationHostedService> _logger;

    public MigrationHostedService(
        IMigrationEngine migrationEngine,
        ILogger<MigrationHostedService> logger)
    {
        _migrationEngine = migrationEngine;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Running database migrations");
        
        try
        {
            var result = await _migrationEngine.UpgradeDatabaseAsync(cancellationToken);
            
            if (result.IsSuccessful)
            {
                _logger.LogInformation(
                    "Successfully applied {Count} migrations. Database version: {Version}",
                    result.AppliedMigrations.Count,
                    result.CurrentVersion);
            }
            else
            {
                _logger.LogError(
                    "Migration failed: {ErrorMessage}",
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migrations");
            throw; // Rethrow to prevent application startup if migrations fail
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

## Common Use Cases

### Applying Migrations to a Specific Version

If you want to migrate to a specific version rather than the latest:

```csharp
// Set target version (migrate to version 2.0)
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("./Migrations")
    .ConfigureForPostgreSql("YourConnectionString")
    .SetUpTargetVersion(new MigrationVersion(2, 0));

var migrationEngine = builder.Build();
await migrationEngine.UpgradeDatabaseAsync();
```

### Running Only Short-Running Migrations

For application startup, you might want to run only short-running migrations:

```csharp
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("./Migrations")
    .ConfigureForPostgreSql("YourConnectionString")
    .UseUpgradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed);
```

### Downgrading to a Previous Version

To downgrade to a specific version:

```csharp
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("./Migrations")
    .ConfigureForPostgreSql("YourConnectionString")
    .UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed)
    .SetUpTargetVersion(new MigrationVersion(1, 0));

var migrationEngine = builder.Build();
await migrationEngine.DowngradeDatabaseAsync();
```

### Creating a Downgrade-Capable Migration

For migrations that can be downgraded:

```csharp
public class AddEmailMigration : CodeMigration, IDowngradeMigration
{
    public override MigrationVersion Version => new(2, 0);
    
    public override string? Comment => "Add email column";
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync(
            "ALTER TABLE Users ADD COLUMN Email VARCHAR(255);",
            null, cancellationToken);
    }
    
    public async Task DowngradeAsync(DbTransaction? transaction = null, 
        CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync(
            "ALTER TABLE Users DROP COLUMN Email;",
            null, cancellationToken);
    }
}
```

## Next Steps

Now that you've got the basics of `Curiosity.Migrations`, check out these resources for more advanced usage:

- [Migration Concepts](./basics.md) - Understanding core concepts
- [Script Migrations](./features/script_migration/index.md) - Detailed information on SQL migrations
- [Code Migrations](./features/code_migration/index.md) - Advanced C# migration techniques
- [Downgrade Migrations](./features/downgrade.md) - How to safely roll back changes
- [Variables](./features/variables.md) - Using variables in migrations
- [Transactions](./features/transactions.md) - Transaction management

## Complete Example Project

Here's a minimal but complete example project structure:

```
MyApp/
├── Program.cs                  # Main application entry point
├── Migrations/                 # SQL migrations
│   ├── 1.0.sql                # Create initial tables
│   └── 2.0.sql                # Add columns or relationships
├── CodeMigrations/            # Code migrations
│   └── AddProfileDataMigration.cs  # C# migration class
└── MyApp.csproj               # Project file with Curiosity.Migrations reference
```

**Program.cs**:
```csharp
using Curiosity.Migrations;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace MyApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup
            var services = new ServiceCollection();
            services.AddLogging(configure => configure.AddConsole());
            
            // Configure migrations
            var builder = new MigrationEngineBuilder(services)
                .UseScriptMigrations().FromDirectory("./Migrations")
                .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly())
                .ConfigureForPostgreSql("Host=localhost;Database=myapp;Username=postgres;Password=secret")
                .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
            
            var migrationEngine = builder.Build();
            
            // Run migrations
            Console.WriteLine("Applying database migrations...");
            var result = await migrationEngine.UpgradeDatabaseAsync();
            
            // Report results
            if (result.IsSuccessful)
            {
                Console.WriteLine($"Applied {result.AppliedMigrations.Count} migrations");
                
                // Your application logic here
                Console.WriteLine("Application ready!");
            }
            else
            {
                Console.WriteLine($"Migration failed: {result.ErrorMessage}");
            }
        }
    }
}
```

This example provides a foundation that you can build upon for your application's specific needs.
