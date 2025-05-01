# Supported databases

<table>
  <tbody>
    <tr>
      <td align="center" valign="middle">
          <img src="https://raw.githubusercontent.com/siisltd/Curiosity.Migrations/master/docs/images/postgresql.png" width="200">
      </td>
      <td align="center" valign="middle">
          <img src="https://upload.wikimedia.org/wikipedia/de/8/8c/Microsoft_SQL_Server_Logo.svg" width="200">
          <br>
          <b>SQL Server</b>
      </td>
    </tr>
  </tbody>
</table>

If you don't find a desired database, you can contribute and add support by yourself.

## PostgreSQL

### Installation

```bash
dotnet add package Curiosity.Migrations.PostgreSQL
```

### Configuration

```csharp
// Configure the migration engine for PostgreSQL
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("./Migrations")
    .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly())
    .ConfigureForPostgreSql("Host=localhost;Database=myapp;Username=postgres;Password=secret")
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);

var migrationEngine = builder.Build();
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| connectionString | PostgreSQL connection string | required |
| migrationTableHistoryName | Name of the table to store migration history | migration_history |
| databaseEncoding | Character set encoding for new database | template database encoding |
| lcCollate | Collation order (LC_COLLATE) for new database | template database value |
| lcCtype | Character classification (LC_CTYPE) for new database | template database value |
| connectionLimit | Max concurrent connections to database | DB default (-1, no limit) |
| template | Template database name for new database creation | template1 |
| tableSpace | Default tablespace for the new database | DB default |

## SQL Server

### Installation

```bash
dotnet add package Curiosity.Migrations.SqlServer
```

### Configuration

```csharp
// Configure the migration engine for SQL Server
var builder = new MigrationEngineBuilder(services)
    .UseScriptMigrations().FromDirectory("./Migrations")
    .UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly())
    .ConfigureForSqlServer("Server=localhost;Database=myapp;User Id=sa;Password=YourStrong@Passw0rd;")
    .UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);

var migrationEngine = builder.Build();
```

### Options

| Option | Description | Default |
|--------|-------------|---------|
| connectionString | SQL Server connection string | required |
| migrationHistoryTableName | Name of table to store migration history | migration_history |
| schemaName | Schema name for migration history table | default user schema (typically dbo) |
| defaultDatabase | Database to connect when target database does not exist | master |
| allowSnapshotIsolation | Whether to enable snapshot isolation | false |
| readCommittedSnapshot | Whether to enable read committed snapshot | false |
| collation | Database collation | server default |
| dataFilePath | Path to data file | SQL Server default |
| logFilePath | Path to log file | SQL Server default |
| initialSize | Initial size of database (MB) | SQL Server default |
| maxSize | Maximum size of database (MB) | SQL Server default |
| fileGrowth | File growth increment (MB) | SQL Server default |
