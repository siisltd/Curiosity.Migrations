using System.Diagnostics.CodeAnalysis;

namespace Curiosity.Migrations.MsSql;

/// <summary>
/// Extension to adding MS SQL migrations to <see cref="MigrationEngineBuilder"/>
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class MigrationEngineBuilderExtensions
{
    /// <summary>
    /// Use provider to make migration on MS SQL Server database.
    /// </summary>
    /// <param name="builder">Migration engine builder</param>
    /// <param name="connectionString">Connection string to MS SQL Server</param>
    /// <param name="migrationTableHistoryName">Migration history table name. If param <see langword="null"/> default value will be used</param>
    /// <param name="schemaName">Schema name for the migration history table. If null, the default schema for the current user will be used.</param>
    /// <param name="defaultDatabase">Default database to use when the target database does not exist or needs to be created. If not provided, the default is 'master'.</param>
    /// <param name="collation">Collation to use when creating a new database. If null, the server's default collation will be used.</param>
    /// <param name="maxConnections">Maximum number of concurrent connections that can be established to the database. If null, SQL Server defaults will be used.</param>
    /// <param name="initialSize">The initial file size for the database's primary data file in MB. If null, SQL Server's default will be used.</param>
    /// <param name="fileGrowth">The autogrowth increment for the database's primary data file in MB. If null, SQL Server's default will be used.</param>
    /// <param name="maxSize">Maximum size that the database can grow to in MB. If null, the database can grow until the disk is full.</param>
    /// <param name="dataFilePath">Path for the primary data file. If null, the SQL Server's default data directory will be used.</param>
    /// <param name="logFilePath">Path for the log file. If null, the SQL Server's default log directory will be used.</param>
    /// <param name="allowSnapshotIsolation">Set the database to be created with snapshot isolation enabled.</param>
    /// <param name="readCommittedSnapshot">Set the database to be created with read committed snapshot isolation enabled.</param>
    /// <remarks>
    /// For detailed params description look at <see cref="MsSqlMigrationConnectionOptions"/>
    /// </remarks>
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static MigrationEngineBuilder ConfigureForMsSql(
        this MigrationEngineBuilder builder,
        string connectionString,
        string? migrationTableHistoryName = null,
        string? schemaName = null,
        string defaultDatabase = "master",
        string? collation = null,
        int? maxConnections = null,
        int? initialSize = null,
        int? fileGrowth = null,
        int? maxSize = null,
        string? dataFilePath = null,
        string? logFilePath = null,
        bool allowSnapshotIsolation = false,
        bool readCommittedSnapshot = false)
    {
        Guard.AssertNotNull(builder, nameof(builder));

        var options = new MsSqlMigrationConnectionOptions(
            connectionString,
            migrationTableHistoryName,
            schemaName,
            defaultDatabase,
            collation,
            maxConnections,
            initialSize,
            fileGrowth,
            maxSize,
            dataFilePath,
            logFilePath,
            allowSnapshotIsolation,
            readCommittedSnapshot);
        
        builder.UseMigrationConnectionFactory(new MsSqlMigrationConnectionFactory(options));
        return builder;
    }
} 