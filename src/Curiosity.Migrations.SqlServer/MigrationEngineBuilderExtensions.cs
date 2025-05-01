using System.Diagnostics.CodeAnalysis;
using Curiosity.Migrations;

namespace Curiosity.Migrations.SqlServer;

/// <summary>
/// Extension methods for <see cref="MigrationEngineBuilder"/> to configure SQL Server provider.
/// </summary>
public static class MigrationEngineBuilderExtensions
{
    /// <summary>
    /// Configures migration engine to use SQL Server provider.
    /// </summary>
    /// <param name="builder">Builder to configure.</param>
    /// <param name="connectionString">Connection string to database.</param>
    /// <param name="migrationHistoryTableName">Name of table to store migration history.</param>
    /// <param name="schemaName">Name of schema to store migration history table.</param>
    /// <param name="defaultDatabase">Name of default database to connect to (master by default).</param>
    /// <param name="allowSnapshotIsolation">Whether to enable snapshot isolation.</param>
    /// <param name="readCommittedSnapshot">Whether to enable read committed snapshot.</param>
    /// <param name="collation">Database collation.</param>
    /// <param name="dataFilePath">Path to data file.</param>
    /// <param name="logFilePath">Path to log file.</param>
    /// <param name="initialSize">Initial size of database in MB.</param>
    /// <param name="maxSize">Maximum size of database in MB.</param>
    /// <param name="fileGrowth">File growth in MB.</param>
    /// <param name="maxConnections">Maximum number of connections.</param>
    /// <returns>Configured builder.</returns>
    /// <remarks>
    /// For detailed params description look at <see cref="SqlServerMigrationConnectionOptions"/>
    /// </remarks>
    public static MigrationEngineBuilder ConfigureForSqlServer(
        this MigrationEngineBuilder builder,
        string connectionString,
        string migrationHistoryTableName = SqlServerMigrationConnectionOptions.DefaultMigrationTableName,
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
        Guard.AssertNotNull(builder, nameof(builder));

        var options = new SqlServerMigrationConnectionOptions(
            connectionString,
            migrationHistoryTableName,
            schemaName,
            defaultDatabase,
            allowSnapshotIsolation,
            readCommittedSnapshot,
            collation,
            dataFilePath,
            logFilePath,
            initialSize,
            maxSize,
            fileGrowth,
            maxConnections);

        builder.UseMigrationConnectionFactory(new SqlServerMigrationConnectionFactory(options));

        return builder;
    }
} 