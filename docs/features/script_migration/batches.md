# Script Migration Batches

Script migrations in `Curiosity.Migrations` can be divided into batches to provide more granular control over SQL script execution. Batches are particularly useful in the following scenarios:

- **Complex Migrations**: When you need to execute multiple SQL operations in a specific order within a single migration.
- **Readability**: To improve script readability by organizing related SQL statements into logical groups.
- **Logging Control**: When you want to track the progress of individual parts of a migration in logs.
- **Error Handling**: To isolate potentially problematic parts of a migration for easier debugging.
- **Performance Management**: When some operations might be long-running and you want to provide better visibility into the migration process.

## How to Enable Batches

Batches are enabled by adding a special comment marker `--BATCH:` followed by an optional batch name in your SQL script. The syntax is:

```sql
--BATCH: [optional_batch_name]
SQL statements for this batch...

--BATCH: [another_batch_name]
SQL statements for another batch...
```

### Example

```sql
--BATCH: Create Tables
CREATE TABLE Users (
    Id INT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL
);

--BATCH: Add Indexes
CREATE INDEX idx_users_email ON Users(Email);
```

When this migration runs, you'll see separate log entries for each batch, making it easier to track execution progress:

```
Executing migration's batch #0 "Create Tables"
Executing migration's batch #1 "Add Indexes"
```

### Important Notes

- Batches within a migration are executed in the order they appear in the script.
- Each batch is processed as a single unit and executed within the same transaction context (if transactions are enabled for the migration).
- Batch names are optional but recommended for better logging and debugging.
- If multiple batches exist in a script, their execution will be logged individually.
- Spaces around the `--BATCH:` marker are ignored (both `--BATCH:` and `-- BATCH:` are valid).