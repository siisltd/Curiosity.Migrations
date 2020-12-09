# Variable Substitution

`Curiosity.Migrations` supports basic variable substitution.
 
## How to use
 
To use variable substitution you should register variables when configuring migrator:

```csharp
var variableValue = "my_variable";
var builder = new MigratorBuilder();
builder.UseVariable("%VARIABLIE%", variableValue);
```

### Script migrations

Then in your database script use this variables:

```sql
-- %VARIABLIE% %AnotherVariable%
SELECT * FROM dbo.%VARIABLIE%
```

After running migrator script will be transformed:

```sql
-- my_variable %AnotherVariable%
SELECT * FROM dbo.my_variable
```

### Code migrations

## Default variables

All providers by default provide next variables:
 
 - `%USER%` - name of user from connection string
 - `%DBNAME%` - database name
