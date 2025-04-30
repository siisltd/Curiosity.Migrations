# Journal

Journal is database table that records the history of all applied migrations. This table is essential for managing database schema changes, ensuring that migrations are applied consistently and accurately across different environments.

#### Key Features

- **Version Tracking**: Each migration entry in the journal includes a version number, which helps in maintaining the correct order and sequence of migrations. This is crucial for ensuring that the database schema evolves as intended.

- **Unique Entries**: The table enforces uniqueness for each migration version, preventing duplicate entries and maintaining the integrity of the migration history. This ensures that each migration is applied only once.

- **Timestamp**: The table records the exact time when each migration was applied, providing a chronological history of changes. This is useful for auditing and understanding the evolution of the database schema over time.

- **Rollback Support**: By maintaining a comprehensive history of applied migrations, the journal facilitates rollback operations. This allows the database to revert to a previous state if necessary, providing flexibility and safety in managing schema changes.

#### Configuration in Curiosity.Migrations

In `Curiosity.Migrations`, the journal is automatically managed by the migration engine. It is created if it does not exist and updated with each applied migration. The table's name can be configured through the `MigrationConnectionOptions`, allowing developers to customize it according to their database naming conventions.

For PostgreSQL, the `PostgresMigrationConnectionOptions` class allows you to specify the `MigrationHistoryTableName`. By default, it is set to `"migration_history"`, but you can provide a custom name when creating an instance of `PostgresMigrationConnectionOptions`.