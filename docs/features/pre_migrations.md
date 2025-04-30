# Pre-migrations

Pre-migrations are a set of migrations that are executed before the main migrations. They are useful for preparing the database environment or performing tasks that must be completed before the main migration logic is applied. Pre-migrations are executed each time the migration engine runs because they are not stored in the migration journal.

Pre-migrations are particularly useful in scenarios where certain setup or configuration tasks need to be completed before the main migration logic is applied. Here are some examples:

- **Setting Up PostgreSQL Extensions**: Use pre-migrations to install or configure PostgreSQL extensions, such as enabling the `uuid-ossp` extension for UUID generation.
- **Seeding Initial Data**: Insert initial data into the database, which is required for the main migrations to function correctly.
- **Configuring Database Settings**: Adjust database settings or parameters that need to be in place before the main migrations are applied.

## Script Pre-migrations

Script pre-migrations allow you to execute raw SQL scripts before the main migration. 


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
