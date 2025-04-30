# Basics

There is some basic concepts you should know about `Curiosity.Migrations`.

## How migrator works

The `Curiosity.Migrations` library facilitates database schema migrations through a structured process. Here's an overview of how the migrator operates:

1. **Configuration**: The migration process begins with configuring the `MigrationEngine` using the `MigrationEngineBuilder`. This involves setting up migration providers, policies, and the target version.

2. **Migration Detection**: The migrator identifies which migrations need to be applied by comparing the current database state with available migrations and the specified target version.

3. **Upgrade and Downgrade**: Depending on the target version, the migrator determines whether to perform an upgrade or downgrade:
   - **Upgrade**: Applies migrations in ascending order up to the target version.
   - **Downgrade**: Applies migrations in descending order down to the target version.

4. **Execution**: The migrator executes the migrations according to the defined policies, ensuring that the database schema is updated correctly.

5. **Logging**: Throughout the process, logging is used to track the progress and any issues that arise.

This structured approach ensures that migrations are applied consistently and in the correct order, maintaining the integrity of the database schema. The `MigrationEngine` uses the configurations to guide the migration process, applying the necessary migrations to reach the desired state.

## Versioning 

`Curiosity.Migrations` allows you to use different ways to version migrations. The version must be sortable and comparable, as the migrator sorts migrations by version to apply them in the correct order.

Version consists of two parts - major and minor numbers separated by a dot - `Major.Minor`.
`Major` is required and usually used to separate different migrations. `Minor` is optional and used to combine migrations into a logical group that must be applied sequentially.

Commonly, the version is just an incrementing value. Some people use a single number to version their migrations, while others use a numbered date format such as `yyyyMMdd`. `Curiosity.Migrations` uses the following regular expression pattern to parse version from a string:

> `([\d\_]+)(\.(\d+))*`

There are examples of valid versions:

- `1` 
- `100.5` 
- `20201012` 
- `20201012_1030` 
- `20201012_1030.21` 

### Upgrading, Downgrading, and Migrating to a Specific Version

To upgrade, downgrade, or migrate to a specific version, you can use the `SetUpTargetVersion` method in the `MigrationEngineBuilder`. This method allows you to specify the target version for the migration process.

Here is an example of how to set up a target version:

```csharp
using Curiosity.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

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

// Perform the migration
await migrationEngine.UpgradeDatabaseAsync();
```

In this example, the migration engine is configured to upgrade the database to version `3`. You can adjust the target version as needed to perform upgrades, downgrades, or migrations to specific versions.

### Migration Detection and Direction

The `Curiosity.Migrations` library determines which migrations to apply and whether to perform an upgrade or downgrade based on the current state of the database and the target version specified.

#### How Migrations are Detected

1. **Current and Target Versions**: The migrator first retrieves the versions of migrations that have already been applied to the database. It then compares these with the available migrations and the specified target version.

2. **Available Migrations**: The migrator maintains a map of available migrations, each associated with a version number. This map is used to determine which migrations are available for application.

3. **Determining Migrations to Apply**:
   - For **upgrades**, the migrator identifies migrations that have not yet been applied and are less than or equal to the target version.
   - For **downgrades**, the migrator identifies migrations that have been applied and are greater than the target version.

4. **Pre-Migrations**: If pre-migrations are specified, these are executed before the main migration process.

#### Upgrade vs. Downgrade

- **Upgrade**: If the target version is greater than the current version, the migrator will apply migrations in ascending order up to the target version.
- **Downgrade**: If the target version is less than the current version, the migrator will apply migrations in descending order down to the target version.

The migrator uses the `SetUpTargetVersion` method to specify the target version, which guides the direction of the migration process.

This process ensures that the database schema is updated correctly according to the specified policies and target version.

### Migration Types

- **Short-Running Migrations**: These are migrations that are expected to complete quickly. They are typically used for small changes that do not require extensive processing time.
  
- **Long-Running Migrations**: These migrations take a longer time to complete and are used for more extensive changes that may involve significant data processing or restructuring.

The `MigrationPolicy` allows you to specify whether short-running or long-running migrations are permitted, providing control over the types of operations that can be performed during the migration process.

## Policy

The `Curiosity.Migrations` library uses a `MigrationPolicy` to control which types of migrations are permitted during the migration process. The policy is defined using a set of flags that specify the allowed operations.

### Available Policies

- **AllForbidden**: No migrations are allowed to run.
- **ShortRunningAllowed**: Only short-running migrations are permitted.
- **LongRunningAllowed**: Only long-running migrations are permitted.
- **AllAllowed**: All migrations, regardless of their duration, are allowed to run.

These policies can be combined using bitwise operations to create custom configurations that suit specific migration requirements.

### Separate Policies for Upgrade and Downgrade

The library allows you to define separate policies for upgrading and downgrading the database schema. This means you can specify different rules for what is allowed during an upgrade versus a downgrade. For example, you might allow all migrations during an upgrade but restrict downgrades to only short-running migrations.

This flexibility ensures that you can tailor the migration process to meet the specific needs and constraints of your application environment.

## Migration Providers

Migration providers are responsible for supplying the migrations that need to be applied to the database. They can source migrations from various locations, such as assemblies or directories containing SQL scripts. The `MigrationEngineBuilder` allows you to configure these providers to suit your application's needs.

For more detailed information on migration providers, please refer to the [Migration Providers](./features/migration_providers.md) article.

The `MigrationEngine` uses these configurations to determine the order and application of migrations, ensuring that the database schema is updated correctly according to the specified policies and target version.


## Configuration

The `Curiosity.Migrations` library allows for configuring the migration process through the `MigrationEngine`. The configuration involves setting up the migration connection, policies for upgrading and downgrading, and specifying target versions if needed.

### Key Configuration Elements

- **Migration Connection**: Establishes the connection to the database where migrations will be applied.
- **Migration Policies**: Define the rules for upgrading and downgrading the database schema.
- **Target Version**: Specifies the version to which the database should be migrated. This can be set to a specific version or left open-ended to apply all available migrations.
- **Pre-Migrations**: Optional scripts that can be executed before the main migration process.

The `MigrationEngine` uses these configurations to determine the order and application of migrations, ensuring that the database schema is updated correctly according to the specified policies and target version.

### Example Configuration

Here is an example of how to configure the `MigrationEngine` using the `MigrationEngineBuilder`:

```csharp
using Curiosity.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Create a service collection
var services = new ServiceCollection();

// Configure the migration engine
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations() // Add script migrations
    .UseCodeMigrations() // Add code migrations
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed) // Set upgrade policy
    .UseDowngradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed) // Set downgrade policy
    .UseLogger(new ConsoleLogger()) // Use a console logger
    .SetUpTargetVersion(new MigrationVersion(1, 0)); // Set target version

// Build the migration engine
var migrationEngine = builder.Build();
```

This example demonstrates how to set up a migration engine with script and code migrations, configure policies for upgrades and downgrades, use a logger, and specify a target version.

### Database and Migration Journal Table Creation

During the migration process, the `Curiosity.Migrations` library ensures that the necessary database and migration journal table are present before applying migrations:

- **Database Creation**: The migrator checks if the database exists. If it does not, the database is created using the `CreateDatabaseIfNotExistsAsync` method.

- **Migration Journal Table Creation**: Once the database is confirmed to exist, the migrator checks for the migration journal table. If it is absent, the table is created using the `CreateMigrationHistoryTableIfNotExistsAsync` method.

These steps ensure that the migration process has the required infrastructure to track and apply migrations effectively.