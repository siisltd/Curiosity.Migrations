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
/// Provides access to underlying database.
/// </summary>
/// <remarks>
/// Abstraction over database connection that allows to used migrator for different database types.
/// </remarks>
public interface IDbProvider : IDisposable
{
    /// <summary>
    /// Uses specified logger to log sql queries.
    /// </summary>
    void UseSqlLogger(ILogger? logger);
        
    /// <summary>
    /// Name of connected database.
    /// </summary>
    string DbName { get; }

    /// <summary>
    /// Connection string to database
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Opened connection to Database. Can be null if method <see cref="OpenConnectionAsync"/> does not called or if <see cref="CloseConnectionAsync"/> was called
    /// </summary>
    DbConnection? Connection { get; }

    /// <summary>
    /// Name of table with applied migration info.
    /// </summary>
    //todo rename to migration history name
    string AppliedMigrationsTableName { get; }

    /// <summary>
    /// Opens connection to database.
    /// </summary>
    Task OpenConnectionAsync(CancellationToken token = default);

    /// <summary>
    /// Begins new DB transaction
    /// </summary>
    /// <returns>DB transaction</returns>
    /// <remarks>
    /// This method was created for easy unit testing
    /// </remarks>
    //todo make async
    DbTransaction BeginTransaction();

    /// <summary>
    /// Creates database with default schema if not exist.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    Task CreateDatabaseIfNotExistsAsync(CancellationToken token = default);

    /// <summary>
    /// Checks if database already exists.
    /// </summary>
    /// <param name="databaseName">Database name</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns><see langword="true"/> if exists, <see langword="false"/> if not</returns>
    /// <exception cref="MigrationException"></exception>
    Task<bool> CheckIfDatabaseExistsAsync(string databaseName, CancellationToken token = default);

    /// <summary>
    /// Creates table for storing applied migrations info if not exist.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    //todo rename to migration history
    Task CreateAppliedMigrationsTableIfNotExistsAsync(CancellationToken token = default);

    /// <summary>
    /// Checks if specified table already exists.
    /// </summary>
    /// <param name="tableName">Name of table to check.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns><see langword="true"/> if exists, <see langword="false"/> if not</returns>
    /// <exception cref="MigrationException"></exception>
    Task<bool> CheckIfTableExistsAsync(string tableName, CancellationToken token = default);

    /// <summary>
    /// Returns applied migrations versions.
    /// </summary>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="InvalidOperationException">If migration history table has incorrect DB version.</exception>
    /// <returns>Collection of applied migration version ordered ascending by version</returns>
    Task<IReadOnlyCollection<DbVersion>> GetAppliedMigrationVersionAsync(CancellationToken token = default);

    /// <summary>
    /// Add migration info to applied migrations table.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    Task SaveAppliedMigrationVersionAsync(string migrationName, DbVersion version, CancellationToken token = default);
        
    /// <summary>
    /// Delete migration info from applied migrations table.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    Task DeleteAppliedMigrationVersionAsync(DbVersion version, CancellationToken token = default);

    /// <summary>
    /// Execute sql script without return value
    /// </summary>
    /// <param name="script">SQL script with DDL or DML commands</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="MigrationException"></exception>
    Task ExecuteScriptAsync(string script, CancellationToken token = default);

    /// <summary>
    /// Execute non-query sql script and return number of modified rows
    /// </summary>
    /// <param name="script">SQL script with DDL or DML commands</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Number of modified rows</returns>
    /// <exception cref="MigrationException"></exception>
    Task<int> ExecuteNonQueryScriptAsync(string script, CancellationToken token = default);

    /// <summary>
    /// Execute sql script and return scalar value
    /// </summary>
    /// <param name="script">SQL script with DDL or DML commands</param>
    /// <param name="token">Cancellation token</param>
    /// <exception cref="MigrationException"></exception>
    Task<object?> ExecuteScalarScriptAsync(string script, CancellationToken token = default);

    /// <exception cref="MigrationException"></exception>
    Task ExecuteScriptWithoutInitialCatalogAsync(string script, CancellationToken token = default);

    /// <exception cref="MigrationException"></exception>
    Task<object?> ExecuteScalarScriptWithoutInitialCatalogAsync(string script, CancellationToken token = default);

    /// <summary>
    /// Close connection to database
    /// </summary>
    /// <returns></returns>
    Task CloseConnectionAsync();

    /// <summary>
    /// Returns default variables from connection string and DB connection
    /// </summary>
    /// <returns>Dictionary with variables. Key - variable name, value - variable value</returns>
    IReadOnlyDictionary<string, string> GetDefaultVariables();
}
