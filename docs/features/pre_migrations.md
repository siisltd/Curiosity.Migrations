# Pre-migrations

Pre-migrations are a set of migrations that are executed before the main migrations. They are useful for preparing the database environment or performing tasks that must be completed before the main migration logic is applied. Pre-migrations are executed each time the migration engine runs because they are not stored in the migration journal.

## Script Pre-migrations

Script pre-migrations allow you to execute raw SQL scripts before the main migration. This can be useful for setting up initial database states or configurations that are required by subsequent migrations.


```csharp
var builder = new MigrationEngineBuilder();
builder.UseScriptPreMigrations().FromDirectory("/path/to/pre-migrations");
```

To use script pre-migrations, you can configure the `MigrationEngineBuilder` to include SQL scripts that should be executed prior to the main migration logic.

## Code Pre-migrations

Code pre-migrations enable you to execute C# code before the main migration. This is particularly useful for complex logic that cannot be easily expressed in SQL.


```csharp
var builder = new MigrationEngineBuilder();
builder.UseCodePreMigrations().Add(new CustomPreMigration());
```

Code pre-migrations can be added to the migration engine using the `MigrationEngineBuilder`, allowing for custom logic to be executed in a controlled manner before the main migrations.

## Integration with Main Migrations

Pre-migrations are integrated into the migration process by being executed before the main migrations. This ensures that any necessary setup or configuration is completed before the main migration logic is applied. The `MigrationEngine` handles the execution of pre-migrations, ensuring they are run in the correct order and that any dependencies are respected.

## When Pre-migrations are Useful

Pre-migrations are particularly useful in scenarios where certain setup or configuration tasks need to be completed before the main migration logic is applied. Here are some examples:

- **Setting Up PostgreSQL Extensions**: Use pre-migrations to install or configure PostgreSQL extensions, such as enabling the `uuid-ossp` extension for UUID generation.
- **Creating Initial Database Structures**: Create necessary tables or schemas that are prerequisites for the main migrations.
- **Seeding Initial Data**: Insert initial data into the database, which is required for the main migrations to function correctly.
- **Configuring Database Settings**: Adjust database settings or parameters that need to be in place before the main migrations are applied.
