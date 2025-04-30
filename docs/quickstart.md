# Quick start

## How to Install

To install `Curiosity.Migrations`, you can use the NuGet package manager. The package is available on NuGet and can be installed using the following command:

```bash
dotnet add package Curiosity.Migrations
```

## How to Configure the Migrator

To configure the migrator, you need to set up the `MigrationEngineBuilder` with the desired options. Here is an example configuration:

```csharp
using Curiosity.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Create a service collection
var services = new ServiceCollection();

// Configure the migration engine
var builder = new MigrationEngineBuilder(services)
    .UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly())
    .UseScriptMigrations().FromDirectory("path/to/migrations")
    .ConfigureForPostgreSql("YourConnectionString")
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed)
    .UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed)
    .SetUpTargetVersion(new MigrationVersion(3)); // Set target version

// Build the migration engine
var migrationEngine = builder.Build();
```

## How to Execute Migrations

To execute migrations, you can use the `IMigrationEngine` interface. Here is how you can upgrade or downgrade the database:

```csharp
// Perform the upgrade
await migrationEngine.UpgradeDatabaseAsync();

// To downgrade
await migrationEngine.DowngradeDatabaseAsync();
```

## Source of Migrations

Migrations can be sourced from different locations, such as assemblies or directories containing SQL scripts. The `MigrationEngineBuilder` allows you to configure these providers:

- **Code Migrations**: Use the `UseCodeMigrations` method to add migrations from assemblies.
- **Script Migrations**: Use the `UseScriptMigrations` method to add migrations from directories.

Example:

```csharp
var builder = new MigrationEngineBuilder(services)
    .UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly())
    .UseScriptMigrations().FromDirectory("path/to/migrations");
```

## Adding Migrations

To add migrations, you can use both SQL scripts and code-based migrations.

### SQL Migrations

SQL migrations are added by placing your SQL scripts in a designated directory. You can configure the directory using the `UseScriptMigrations` method in the `MigrationEngineBuilder`.

Example:

```csharp
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("path/to/sql/migrations");
```

For more detailed information, refer to the [Script Migrations](./features/script_migration/index.md) article.

### Code Migrations

Code migrations are written in C# and allow for more complex logic that is difficult to achieve with plain SQL. You can add code migrations by specifying the assembly containing the migration classes using the `UseCodeMigrations` method.

Example:

```csharp
var builder = new MigrationEngineBuilder(services)
    .UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly());
```

For more detailed information, refer to the [Code Migrations](./features/code_migration/index.md) article.

#### Example of a Code Migration

Here is a simple example of a code migration:

```csharp
using Curiosity.Migrations;
using System.Threading;
using System.Threading.Tasks;

public class AddUsersTableMigration : CodeMigration
{
    public override MigrationVersion Version => new MigrationVersion(1, 0);

    public override string? Comment => "Add Users table";

    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        var sql = @"CREATE TABLE Users (
            Id INT PRIMARY KEY,
            Name NVARCHAR(100) NOT NULL,
            Email NVARCHAR(100) NOT NULL
        );";

        await MigrationConnection.ExecuteNonQuerySqlAsync(sql, null, cancellationToken);
    }
}
```

This migration creates a new table called `Users` with columns for `Id`, `Name`, and `Email`. The `UpgradeAsync` method contains the logic to apply the migration.

### Adding Migrator to Dependency Injection

To add the migrator directly into the Dependency Injection (DI) container, you can use the `AddMigrations` extension method. This method configures the `MigrationEngineBuilder` and adds the migration engine to the DI container.

Here is an example of how to use the `AddMigrations` method:

```csharp
using Curiosity.Migrations;
using Microsoft.Extensions.DependencyInjection;

// Create a service collection
var services = new ServiceCollection();

// Add migrations to the service collection
services.AddMigrations(builder =>
{
    builder.UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly())
           .UseScriptMigrations().FromDirectory("path/to/migrations")
           .ConfigureForPostgreSql("YourConnectionString")
           .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed)
           .UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed)
           .SetUpTargetVersion(new MigrationVersion(3)); // Set target version
});
```

This example demonstrates how to set up the `MigrationEngineBuilder` with both code and script migrations, configure it for PostgreSQL, and add it to the DI container using the `AddMigrations` method. This setup ensures that the migration engine is available throughout the application's lifecycle.
