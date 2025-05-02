# Curiosity.Migrations [![Build Status](https://github.com/siisltd/Curiosity.Migrations/actions/workflows/build.yml/badge.svg?branch=master)](https://github.com/siisltd/Curiosity.Migrations/actions/workflows/build.yml) [![License](https://img.shields.io/github/license/siisltd/curiosity.migrations.svg)](https://github.com/siisltd/Curiosity.Migrations/blob/master/LICENSE) [![NuGet Downloads](https://img.shields.io/nuget/dt/Curiosity.Migrations)](https://www.nuget.org/packages/Curiosity.Migrations) [![Documentation Status](https://readthedocs.org/projects/curiosity-migrations/badge/?version=latest)](https://curiosity-migrations.readthedocs.io/)

## Introduction

Curiosity.Migrations is a powerful, flexible database migration framework for .NET and .NET Core applications that gives you precise control over how your database evolves. It combines the performance and control of raw SQL with the flexibility of C# code migrations, all wrapped in a robust, enterprise-ready migration system.

Unlike ORM-specific migration tools, Curiosity.Migrations is database-focused and designed for scenarios where you need fine-grained control over migration execution, especially for large production databases where performance and safety are critical.

## Why Use Curiosity.Migrations?

<table>
  <tr>
    <td width="50%" valign="top">
      <h3>🔧 Precise Control</h3>
      <p>Write raw SQL when you need optimal performance, or use C# code when you need complex logic. You control exactly what runs against your database.</p>
    </td>
    <td width="50%" valign="top">
      <h3>🚀 Production-Ready</h3>
      <p>Built for enterprise applications with safety features, long-running migration support, and granular policies to control what runs in each environment.</p>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>🔄 Bidirectional</h3>
      <p>First-class support for downgrade migrations enables safe rollbacks when deployments don't go as planned.</p>
    </td>
    <td width="50%" valign="top">
      <h3>📊 Progressive Migrations</h3>
      <p>Separate long-running data migrations from quick schema changes to keep your application responsive during upgrades.</p>
    </td>
  </tr>
  <tr>
    <td width="50%" valign="top">
      <h3>🧪 Testability</h3>
      <p>Create and initialize test databases with specific migration states for reliable integration testing.</p>
    </td>
    <td width="50%" valign="top">
      <h3>🧩 Extensibility</h3>
      <p>Customize where migrations come from, how they're logged, and how they're applied to fit your workflow.</p>
    </td>
  </tr>
</table>

## Core Features

### Migration Types

- **[Script Migrations](https://curiosity-migrations.readthedocs.io/features/script_migration/index)**: Write raw SQL for direct database access
    - [Batched Execution](https://curiosity-migrations.readthedocs.io/features/script_migration/batches): Split large scripts into manageable chunks
    - Full support for database-specific SQL features and optimizations

- **[Code Migrations](https://curiosity-migrations.readthedocs.io/features/code_migration/index)**: Implement migrations in C# for complex scenarios
    - [Dependency Injection](https://curiosity-migrations.readthedocs.io/features/code_migration/di): Use your application's services in migrations
    - [Entity Framework Integration](https://curiosity-migrations.readthedocs.io/features/code_migration/ef_integration): Leverage EF Core when needed
    - Implement custom validation, logging, or business logic during migrations

### Safety and Control

- **[Policies](https://curiosity-migrations.readthedocs.io/basics#migration-policies)**: Control which migrations run in different environments
- **[Dependencies](https://curiosity-migrations.readthedocs.io/features/dependencies)**: Specify explicit requirements between migrations
- **[Downgrade Migrations](https://curiosity-migrations.readthedocs.io/features/downgrade)**: Safely roll back changes when needed
- **[Transactions](https://curiosity-migrations.readthedocs.io/features/transactions)**: Configure transaction behavior per migration
- **Long-running vs Short-running**: Separate quick schema changes from data-intensive operations

### Extensibility

- **[Migration Providers](https://curiosity-migrations.readthedocs.io/features/migration_providers)**: Source migrations from files, embedded resources, etc.
- **[Variables](https://curiosity-migrations.readthedocs.io/features/variables)**: Dynamic value substitution in migrations
- **[Pre-migrations](https://curiosity-migrations.readthedocs.io/features/pre_migrations)**: Run setup scripts before main migrations
- **[Custom Journal](https://curiosity-migrations.readthedocs.io/features/journal)**: Configure how applied migrations are tracked

## Quick Start

### Installation

```bash
# Install core package
dotnet add package Curiosity.Migrations

# Install database provider (PostgreSQL or SQL Server)
dotnet add package Curiosity.Migrations.PostgreSQL
# or
dotnet add package Curiosity.Migrations.SqlServer
```

### Basic Setup

```csharp
// Configure the migration engine
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("./Migrations")  // Add SQL migrations
    .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly())  // Add code migrations
    .ConfigureForPostgreSql("Host=localhost;Database=myapp;Username=postgres;Password=secret")
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);

// Build and run the engine
var migrationEngine = builder.Build();
var result = await migrationEngine.UpgradeDatabaseAsync();

// Check results
if (result.IsSuccessful)
{
    Console.WriteLine($"Successfully migrated");
}
```

Get started quickly with the [**Quick Start Guide**](https://curiosity-migrations.readthedocs.io/quickstart) or dive into [**Core Concepts**](https://curiosity-migrations.readthedocs.io/basics).

## Supported Databases

<table>
  <tbody>
    <tr>
      <td align="center" valign="middle">
        <img src="https://raw.githubusercontent.com/siisltd/Curiosity.Migrations/refs/heads/master/docs/images/postgresql.png" width="200">
        <br>
        <b>PostgreSQL</b>
      </td>
      <td align="center" valign="middle">
        <img src="https://raw.githubusercontent.com/siisltd/Curiosity.Migrations/refs/heads/master/docs/images/sqlserver.svg" width="200">
        <br>
        <b>SQL Server</b>
      </td>
    </tr>
  </tbody>
</table>

Support for additional databases can be added through contributions.

## Comparing with Alternatives

<table>
  <thead>
    <tr>
      <th>Feature</th>
      <th>Curiosity.Migrations</th>
      <th>EF Core Migrations</th>
      <th>FluentMigrator</th>
      <th>DbUp</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Direct SQL Control</td>
      <td>✅ Full</td>
      <td>⚠️ Generated</td>
      <td>⚠️ Generated</td>
      <td>✅ Full</td>
    </tr>
    <tr>
      <td>Code Migrations</td>
      <td>✅ Native</td>
      <td>⚠️ Limited</td>
      <td>⚠️ Via API</td>
      <td>❌ No</td>
    </tr>
    <tr>
      <td>Downgrade Support</td>
      <td>✅ First-class</td>
      <td>⚠️ Limited</td>
      <td>⚠️ Limited</td>
      <td>❌ No</td>
    </tr>
    <tr>
      <td>Long-running Migrations</td>
      <td>✅ Optimized</td>
      <td>❌ No</td>
      <td>❌ No</td>
      <td>❌ No</td>
    </tr>
    <tr>
      <td>Migration Policies</td>
      <td>✅ Configurable</td>
      <td>❌ No</td>
      <td>❌ No</td>
      <td>❌ No</td>
    </tr>
    <tr>
      <td>DI Support</td>
      <td>✅ Native</td>
      <td>⚠️ Limited</td>
      <td>⚠️ Limited</td>
      <td>⚠️ Basic</td>
    </tr>
    <tr>
      <td>Best For</td>
      <td>Enterprise apps, complex migrations, performance-critical systems</td>
      <td>Simple CRUD apps with EF Core</td>
      <td>Cross-database projects</td>
      <td>Simple script runners</td>
    </tr>
  </tbody>
</table>

For more detailed comparisons, see [The Philosophy Behind Curiosity.Migrations](https://curiosity-migrations.readthedocs.io/philosophy).

## Available Packages

| Package | Version | Downloads |
|---------|---------|-----------|
| Curiosity.Migrations | [![NuGet](https://img.shields.io/nuget/v/Curiosity.Migrations.svg)](https://www.nuget.org/packages/Curiosity.Migrations/) | [![NuGet](https://img.shields.io/nuget/dt/Curiosity.Migrations)](https://www.nuget.org/packages/Curiosity.Migrations) |
| Curiosity.Migrations.PostgreSQL | [![NuGet](https://img.shields.io/nuget/v/Curiosity.Migrations.PostgreSQL.svg)](https://www.nuget.org/packages/Curiosity.Migrations.PostgreSQL/) | [![NuGet](https://img.shields.io/nuget/dt/Curiosity.Migrations.PostgreSQL)](https://www.nuget.org/packages/Curiosity.Migrations.PostgreSQL) |
| Curiosity.Migrations.SqlServer | [![NuGet](https://img.shields.io/nuget/v/Curiosity.Migrations.SqlServer.svg)](https://www.nuget.org/packages/Curiosity.Migrations.SqlServer/) | [![NuGet](https://img.shields.io/nuget/dt/Curiosity.Migrations.SqlServer)](https://www.nuget.org/packages/Curiosity.Migrations.SqlServer) |

## Documentation

* [Quick Start Guide](https://curiosity-migrations.readthedocs.io/quickstart) - Getting started with Curiosity.Migrations
* [Core Concepts](https://curiosity-migrations.readthedocs.io/basics) - Understanding the fundamental concepts
* [The Philosophy Behind Curiosity.Migrations](https://curiosity-migrations.readthedocs.io/idea) - Why this library exists
* [Feature Documentation](#core-features) - Detailed guides for each feature
* [Supported Databases](https://curiosity-migrations.readthedocs.io/supported_databases) - Currently supported database systems

## Community and Support

* [GitHub Issues](https://github.com/siisltd/Curiosity.Migrations/issues) - Report bugs or request features
* [GitHub Discussions](https://github.com/siisltd/Curiosity.Migrations/discussions) - Ask questions and discuss ideas

## License

Curiosity.Migrations is licensed under the [MIT License](https://github.com/siisltd/Curiosity.Migrations/blob/master/LICENSE).
