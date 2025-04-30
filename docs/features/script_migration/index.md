# Script migration

Script migrations are used to apply changes to a database using raw SQL scripts. Script migrations can be organized into batches, allowing for more granular control over the execution order and transaction management.

To use script migrations, you can define them in SQL files and organize them in directories or embed them in assemblies. The `ScriptMigrationsProvider` class facilitates the discovery and execution of these scripts from specified locations.

For more advanced scenarios, you can create custom script providers by implementing the `IMigrationsProvider` interface. This allows you to tailor the migration process to your specific requirements (for more detail read [Migration Providers](../migration_providers.md) article).

## File naming

Migration scripts must follow a specific naming pattern to be recognized and processed correctly. The required pattern is defined by the following regular expression:

```csharp
^([\d_]+)(\.(\d+))*.(down|up)?(-([\w]*))?\.sql$
```

- **Version**: The script name must start with a version number, which can include underscores and dots.
- **Direction**: Optionally, the script can specify a direction (`up` or `down`) to indicate whether it is an upgrade or downgrade script.
- **Comment**: A comment can be included at the end of the file name, prefixed by a dash.

If a script file does not match this pattern, the behavior can be configured using the `ScriptIncorrectNamingAction` enum, which provides the following options:

- `Ignore`: The script is ignored, and the process continues with the next scripts.
- `LogToWarn`: A warning is logged, and the process continues.
- `LogToError`: An error is logged, and the process continues.
- `ThrowException`: An exception is thrown, aborting the execution.