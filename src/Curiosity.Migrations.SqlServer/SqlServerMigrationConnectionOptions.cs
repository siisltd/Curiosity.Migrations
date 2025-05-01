using Curiosity.Migrations;

namespace Curiosity.Migrations.SqlServer;

/// <summary>
/// Options for <see cref="SqlServerMigrationConnection"/>
/// </summary>
public class SqlServerMigrationConnectionOptions : IMigrationConnectionOptions
{
    /// <summary>
    /// Default value for <see cref="MigrationHistoryTableName"/>
    /// </summary>
    public const string DefaultMigrationTableName = "migration_history";
    
    /// <summary>
    /// Default schema name in SQL Server
    /// </summary>
    public const string DefaultSchemaName = "dbo";

    /// <inheritdoc />
    public string ConnectionString { get; }

    /// <inheritdoc />
    public string MigrationHistoryTableName { get; }

    /// <summary>
    /// Schema name for the migration history table.
    /// If null, the default schema for the current user will be used.
    /// </summary>
    public string? SchemaName { get; }

    /// <summary>
    /// Default database to use when the target database does not exist or needs to be created.
    /// If not provided, the default is 'master'.
    /// </summary>
    public string DefaultDatabase { get; }

    /// <summary>
    /// Collation to use when creating a new database.
    /// If null, the server's default collation will be used.
    /// </summary>
    public string? Collation { get; }

    /// <summary>
    /// Maximum number of concurrent connections that can be established to the database.
    /// If null, SQL Server defaults will be used.
    /// </summary>
    public int? MaxConnections { get; }
    
    /// <summary>
    /// The initial file size for the database's primary data file in MB.
    /// If null, SQL Server's default will be used.
    /// </summary>
    public int? InitialSize { get; }
    
    /// <summary>
    /// The autogrowth increment for the database's primary data file in MB.
    /// If null, SQL Server's default will be used.
    /// </summary>
    public int? FileGrowth { get; }
    
    /// <summary>
    /// Maximum size that the database can grow to in MB. 
    /// If null, the database can grow until the disk is full.
    /// </summary>
    public int? MaxSize { get; }
    
    /// <summary>
    /// Path for the primary data file. 
    /// If null, the SQL Server's default data directory will be used.
    /// </summary>
    public string? DataFilePath { get; }
    
    /// <summary>
    /// Path for the log file.
    /// If null, the SQL Server's default log directory will be used.
    /// </summary>
    public string? LogFilePath { get; }
    
    /// <summary>
    /// Set the database to be created with snapshot isolation enabled.
    /// </summary>
    public bool AllowSnapshotIsolation { get; }
    
    /// <summary>
    /// Set the database to be created with read committed snapshot isolation enabled.
    /// </summary>
    public bool ReadCommittedSnapshot { get; }

    /// <inheritdoc cref="SqlServerMigrationConnectionOptions"/>
    public SqlServerMigrationConnectionOptions(
        string connectionString,
        string migrationHistoryTableName = DefaultMigrationTableName,
        string? schemaName = null,
        string? defaultDatabase = null,
        bool allowSnapshotIsolation = false,
        bool readCommittedSnapshot = false,
        string? collation = null,
        string? dataFilePath = null,
        string? logFilePath = null,
        int? initialSize = null,
        int? maxSize = null,
        int? fileGrowth = null,
        int? maxConnections = null)
    {
        SqlServerGuard.AssertConnectionString(connectionString, nameof(connectionString));
        SqlServerGuard.AssertTableName(migrationHistoryTableName, nameof(migrationHistoryTableName));
        Guard.AssertNotEmpty(defaultDatabase, nameof(defaultDatabase));

        ConnectionString = connectionString;
        MigrationHistoryTableName = migrationHistoryTableName;
        SchemaName = schemaName;
        DefaultDatabase = defaultDatabase;
        Collation = collation;
        MaxConnections = maxConnections;
        InitialSize = initialSize;
        FileGrowth = fileGrowth;
        MaxSize = maxSize;
        DataFilePath = dataFilePath;
        LogFilePath = logFilePath;
        AllowSnapshotIsolation = allowSnapshotIsolation;
        ReadCommittedSnapshot = readCommittedSnapshot;
    }
} 