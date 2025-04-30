# Transactions

Transactions are a fundamental aspect of database migrations, ensuring that changes are applied consistently and reliably. They allow a series of operations to be grouped into a single unit of work, which is either fully completed or fully rolled back. This atomicity is crucial for maintaining data integrity, especially in complex migrations involving multiple steps or operations.

## Importance of Transactions

- **Data Integrity**: Transactions ensure that all operations within a migration are completed successfully before committing the changes. If any operation fails, the transaction can be rolled back, leaving the database in a consistent state.
- **Error Handling**: By using transactions, errors can be handled more gracefully. If an error occurs during a migration, the transaction can be rolled back, preventing partial updates that could lead to data corruption.
- **Concurrency Control**: Transactions help manage concurrent access to the database, ensuring that multiple migrations or operations do not interfere with each other.
- **Consistency**: Transactions ensure that the database remains in a consistent state, even in the event of a failure. This is particularly important in environments where multiple migrations may be applied simultaneously.

By default, transactions are enabled for both script and code migrations. This means that each migration is executed within its own transaction, providing the benefits of atomicity, consistency, and error handling. However, there are scenarios where you might want to disable transactions, such as when performing operations that cannot be executed within a transaction, like creating indexes concurrently.

The following sections provide detailed information on how to manage transactions in both script and code migrations.

## Transaction in a Script Migration

In script migrations, transactions are managed using placeholders within the SQL script. The `ScriptMigrationsProvider` class processes these scripts and extracts options using specific placeholders, allowing for dynamic control over transaction management directly within the SQL script.

To manage transactions in script migrations:

- **Enable Transactions**: Use the placeholder `-- CURIOSITY: TRANSACTION = ON` within your SQL script to enable transactions.
- **Disable Transactions**: Use the placeholder `-- CURIOSITY: TRANSACTION = OFF` to disable transactions.

Example of managing transactions in a script migration:

```sql
-- CURIOSITY: TRANSACTION = OFF
-- Your SQL script here
```

In this example, the transaction is disabled for the SQL script by using the `TRANSACTION = OFF` placeholder.

## Transaction in a Code Migration

In code migrations, transactions are managed programmatically using the `IsTransactionRequired` property of `CodeMigration` class. 

To manage transactions in code migrations:

- **Enable Transactions**: By default, transactions are enabled. This means that the migration engine will create a separate transaction for each migration. The transaction is passed as an argument to the `UpgradeAsync` method.
- **Disable Transactions**: Transactions can be disabled by setting the `IsTransactionRequired` property to `false`. In this case, the transaction argument in the `UpgradeAsync` method will be `null`. This is useful when you need to manually manage transactions or when performing operations that cannot be executed within a transaction, such as creating indexes concurrently.

You can also create your own transactions within the migration, for example, for each mini-step of the migration process.

Example of disabling a transaction in a code migration:

```csharp
public class MyCodeMigration : CodeMigration
{
    public override MigrationVersion Version => new MigrationVersion(1, 0, 0);
    public override string? Comment => "Example migration";

    public MyCodeMigration()
    {
        IsTransactionRequired = false; // Disable transaction
    }

    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        // Example of creating a custom transaction for a mini-step
        using (var customTransaction = MigrationConnection.BeginTransaction())
        {
            // Your migration logic here
            customTransaction.Commit();
        }
    }
}
```

In this example, the `IsTransactionRequired` property is set to `false`, disabling the transaction for this specific migration. However, a custom transaction is created within the `UpgradeAsync` method for a specific mini-step.