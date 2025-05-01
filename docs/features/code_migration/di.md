# Dependency Injection

The `Curiosity.Migrations` library supports Dependency Injection (DI) to facilitate the creation and management of migration classes. This is primarily achieved through the `CodeMigrationsProvider`. This class is responsible for discovering and creating code migrations. It utilizes the `IServiceCollection` to register migration types, enabling the DI container to resolve and inject dependencies into migration classes.

## Usage

To use DI with `CodeMigrationsProvider`, follow these steps:

1. **Register Migration Classes**: Use the `CodeMigrationsProvider` to register migration classes with the DI container. This allows the DI system to inject dependencies into these classes when they are instantiated.

   ```csharp
   var services = new ServiceCollection();
   var provider = new CodeMigrationsProvider(services);
   provider.FromAssembly(typeof(YourMigrationClass).Assembly);
   ```

2. **Inject Dependencies**: Define your migration classes to accept dependencies through their constructors. The DI container will automatically resolve and inject these dependencies when the migration is executed.

   ```csharp
   public class YourMigrationClass : CodeMigration
   {
       private readonly IDependency _dependency;

       public YourMigrationClass(IDependency dependency)
       {
           _dependency = dependency;
       }

       public override Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
       {
           // Use _dependency here
       }
   }
   ```

3. **Build and Run**: Ensure that your DI container is properly configured and that the migration engine is built and executed as needed.
