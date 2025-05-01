# Migration Providers

Migration providers are responsible for supplying migrations from various sources to the migration engine. They implement the `IMigrationsProvider` interface, which defines a method to return a collection of migrations.

The purpose of a migration provider is to abstract the source of migrations, allowing the migration engine to apply them without needing to know their origin. This enables flexibility in how migrations are defined and retrieved, whether from code, scripts, or other sources.

## Existed providers

### CodeMigrationsProvider

The `CodeMigrationsProvider` is designed to handle migrations written in C#. Migrations are sourced from specified assemblies, allowing for a structured and organized approach to migration management.

#### Methods

- **FromAssembly**
  
    Use this method to set up an assembly for scanning migrations. This is useful when you want to include all migrations from a specific assembly.

    ```csharp
    var builder = new MigrationEngineBuilder(services)
        .UseCodeMigrations()
        .FromAssembly(assembly);
    ```

- **FromAssembly<T>**
  
    Use this method to set up an assembly for scanning migrations with a specified type. This is beneficial when you want to filter migrations by a specific type within an assembly.

    ```csharp
    var builder = new MigrationEngineBuilder(services)
        .UseCodeMigrations()
        .FromAssembly<MyMigrationType>(assembly);
    ```

### ScriptMigrationsProvider

The `ScriptMigrationsProvider` is designed to handle migrations using raw SQL scripts. This provider is ideal for straightforward SQL-based migrations and can scan directories or assemblies for script files, allowing for a flexible approach to migration management.

#### Methods

- **FromDirectory**
  
    Use this method to set up a directory for scanning migrations. This is useful when you want to include all script migrations from a specific directory.
  
    ```csharp
    var builder = new MigrationEngineBuilder(services)
        .UseScriptMigrations()
        .FromDirectory("/path/to/scripts");
    ```

- **FromAssembly**
  
    Use this method to set up an assembly where script migrations are embedded. This is beneficial when you want to include script migrations from embedded resources within an assembly.
  
    ```csharp
    var builder = new MigrationEngineBuilder(services)
        .UseScriptMigrations()
        .FromAssembly(assembly, "MyNamespace");
    ```

## How to add custom provider

To add a custom migrations provider, use the `UseCustomMigrationsProvider` method of the `MigrationEngineBuilder` class. This method allows you to specify your own implementation of the `IMigrationsProvider` interface.

```csharp
public MigrationEngineBuilder UseCustomMigrationsProvider(IMigrationsProvider provider)
```

- **provider**: An instance of a class that implements the `IMigrationsProvider` interface.

This method adds the custom provider to the list of migrations providers used by the migration engine.

