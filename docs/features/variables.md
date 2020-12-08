# Variable Substitution

`Curiosity.Migrations` supports basic variable substitution, to enable you should register variables when configuring migrator:

```csharp
var variableValue = "my_variable";
var builder = new MigratorBuilder();
builder.UseVariable("%VARIABLIE%", variableValue);
```

Then in your database script:

```sql
-- %VARIABLIE% %AnotherVariable%
SELECT * FROM dbo.%VARIABLIE%
```

Will execute:

```sql
-- my_variable %AnotherVariable%
SELECT * FROM dbo.my_variable
```

All providers by default provide next variables:
 
 - `%USER%` - name of user from connection string
 - `%DBNAME%` - database name
