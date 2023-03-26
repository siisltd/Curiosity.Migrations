using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace Curiosity.Migrations;

/// <summary>
/// Connection to a database for migration.
/// </summary>
/// <remarks>
/// Facade over real database connection that allows connection be used by migrator for different database types.
/// Implements some common operations for migrations (database creation, table creation)
/// and logs executed SQL queries if logger if specified.
/// </remarks>
public interface IMigrationConnection : IDisposable
{
    /// <summary>
    /// Name of connected database.
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Connection string to database.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Opened connection to Database.
    /// </summary>
    /// <remarks>
    /// Can be null if method <see cref="OpenConnectionAsync"/> does not called or if <see cref="CloseConnectionAsync"/> was called.
    /// </remarks>
    DbConnection? Connection { get; }

    /// <summary>
    /// Name of table with applied migration info.
    /// </summary>
    string MigrationHistoryTableName { get; }

    /// <summary>
    /// Uses specified logger to log sql queries.
    /// </summary>
    void UseSqlLogger(ILogger? logger);

    /// <summary>
    /// Opens connection to database.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    Task OpenConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins new DB transaction.
    /// </summary>
    /// <returns>DB transaction</returns>
    // there is no Async version of BeginTransaction method on DbConnection,
    // don't try to add async support here. 
    DbTransaction BeginTransaction();

    /// <summary>
    /// Creates database with default schema if not exist.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    Task CreateDatabaseIfNotExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Safely checks if database already exists.
    /// </summary>
    /// <param name="databaseName">Database name</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if exists, <see langword="false"/> if not</returns>
    /// <exception cref="MigrationException"></exception>
    Task<bool> CheckIfDatabaseExistsAsync(
        string databaseName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates table for storing applied migrations info if not exist.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    Task CreateMigrationHistoryTableIfNotExistsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if specified table already exists.
    /// </summary>
    /// <param name="tableName">Name of table to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> if exists, <see langword="false"/> if not</returns>
    /// <exception cref="MigrationException"></exception>
    Task<bool> CheckIfTableExistsAsync(
        string tableName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns applied migrations versions from migration history table.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="InvalidOperationException">If migration history table has incorrect DB version.</exception>
    /// <returns>Collection of applied migration version ordered ascending by version</returns>
    Task<IReadOnlyCollection<DbVersion>> GetAppliedMigrationVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds applied migration info to migrations history table.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    Task SaveAppliedMigrationVersionAsync(
        DbVersion version,
        string migrationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes applied migration info from migrations history table.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    Task DeleteAppliedMigrationVersionAsync(
        DbVersion version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes sql script.
    /// </summary>
    /// <param name="sqlQuery">SQL script with DDL or DML commands</param>
    /// <param name="queryParams">Optional query params.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of modified rows</returns>
    /// <exception cref="MigrationException"></exception>
    Task<int> ExecuteNonQuerySqlAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes sql script and returns scalar value.
    /// </summary>
    /// <param name="sqlQuery">SQL script with DDL or DML commands</param>
    /// <param name="queryParams">Optional query params.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="MigrationException"></exception>
    Task<object> ExecuteScalarSqlAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes sql script on a default database and returns scalar value.
    /// </summary>
    /// <inheritdoc cref="ExecuteScalarSqlAsync"/>
    Task<int> ExecuteNonQuerySqlWithoutInitialCatalogAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes SQL query with returned valued on a default database.
    /// </summary>
    /// <param name="sqlQuery">SQL query toe execute.</param>
    /// <param name="queryParams">Optional query params.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="MigrationException"></exception>
    Task<object> ExecuteScalarSqlWithoutInitialCatalogAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Closes connection to a database.
    /// </summary>
    Task CloseConnectionAsync();

    /// <summary>
    /// Returns default variables from connection string and DB connection
    /// </summary>
    /// <returns>Dictionary with variables. Key - variable name, value - variable value</returns>
    IReadOnlyDictionary<string, string> GetDefaultVariables();
}
