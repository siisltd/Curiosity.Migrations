# Variables and variable substitution

`Curiosity.Migrations` supports basic variable substitution.
 
Variables in migrations provide several important benefits:

- **Environment independence**: Create migrations that work across different environments (development, staging, production) without code changes
- **Improved maintainability**: Centralize configuration values instead of hardcoding them throughout migrations
- **Reduced errors**: Avoid typos and inconsistencies by defining values once and reusing them
- **Better security**: Keep sensitive information out of migration scripts by injecting them as variables

## How to use
 
To use variable substitution you should register variables when configuring migrator:

```csharp
var variableValue = "my_variable";
var builder = new MigratorBuilder();
builder.UseVariable("%VARIABLIE%", variableValue);
```

### Script migrations

In script migrations, preprocessing is performed to substitute variables with their values before execution.

```sql
-- %VARIABLIE% %NotExistedVariable%
SELECT * FROM dbo.%VARIABLIE%
```

After running migrator script will be transformed:

```sql
-- my_variable %NotExistedVariable%
SELECT * FROM dbo.my_variable
```

### Code migrations

In code migrations, variables are accessible via the `Variables` property in your migration class. This property is a `IReadOnlyDictionary<string, string>` which is populated automatically when your migration is initialized.

```csharp
public class MyCodeMigration : CodeMigration
{
    public override MigrationVersion Version => new MigrationVersion(1, 0, 0);
    
    public override string? Comment => "My migration with variables";
    
    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // Access variables using the Variables dictionary
        var user = Variables[DefaultVariables.User];
        var dbName = Variables[DefaultVariables.DbName];
        var customVariable = Variables["%VARIABLIE%"];
        
        // Use variables in your migration logic
        Logger?.LogInformation($"Running migration as user {user} on database {dbName}");
        
        // Custom SQL with variables
        var sql = $"CREATE TABLE {customVariable}_table (id INT)";
        await MigrationConnection.ExecuteNonQueryAsync(sql, transaction, cancellationToken);
    }
}
```

## Default variables

All providers by default provide next variables:
 
 - `%USER%` - name of user from connection string
 - `%DBNAME%` - database name
