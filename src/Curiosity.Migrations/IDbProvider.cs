using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Provide access to underlying database
    /// </summary>
    public interface IDbProvider : IDisposable
    {
        /// <summary>
        /// Use specified logger to log sql queries
        /// </summary>
        void UseSqlLogger(ILogger? logger);
        
        /// <summary>
        /// Name of connected database
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
        /// Name of table with migration history
        /// </summary>
        string MigrationHistoryTableName { get; }

        /// <summary>
        /// Open connection to database
        /// </summary>
        /// <returns></returns>
        Task OpenConnectionAsync(CancellationToken token = default);

        /// <summary>
        /// Begins new DB transaction
        /// </summary>
        /// <returns>DB transaction</returns>
        /// <remarks>
        /// This method was created for easy unit testing
        /// </remarks>
        DbTransaction BeginTransaction();

        /// <summary>
        /// Returns actual DB state
        /// </summary>
        /// <param name="desiredVersion">Version of the newest migration</param>
        /// <param name="isDowngradeEnabled">Indicates that downgrade is enabled for migrator. Affects how migration history table is analyzed.</param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<DbState> GetDbStateSafeAsync(DbVersion desiredVersion, bool isDowngradeEnabled, CancellationToken token = default);

        /// <summary>
        /// Create database with default schema if not exist
        /// </summary>
        /// <exception cref="MigrationException"></exception>
        Task CreateDatabaseIfNotExistsAsync(CancellationToken token = default);

        /// <summary>
        /// Check if database already exists
        /// </summary>
        /// <param name="databaseName">Database name</param>
        /// <param name="token"></param>
        /// <returns><see langword="true"/> if exists, <see langword="false"/> if not</returns>
        /// <exception cref="MigrationException"></exception>
        Task<bool> CheckIfDatabaseExistsAsync(string databaseName, CancellationToken token = default);

        /// <summary>
        /// Create table for storing migration history info no exist
        /// </summary>
        /// <exception cref="MigrationException"></exception>
        Task CreateHistoryTableIfNotExistsAsync(CancellationToken token = default);

        /// <summary>
        /// Check if table already exists
        /// </summary>
        /// <param name="tableName">Table name</param>
        /// <param name="token"></param>
        /// <returns><see langword="true"/> if exists, <see langword="false"/> if not</returns>
        /// <exception cref="MigrationException"></exception>
        Task<bool> CheckIfTableExistsAsync(string tableName, CancellationToken token = default);

        /// <summary>
        /// Returns actual database version from migration history table.
        /// </summary>
        /// <param name="isDowngradeEnabled">Indicates that downgrade is enabled for migrator. Affects how migration history table is analyzed.</param>   
        /// <param name="token">Cancellation token</param>
        /// <exception cref="InvalidOperationException">If migration history table has incorrect DB version.</exception>
        Task<DbVersion?> GetDbVersionAsync(bool isDowngradeEnabled, CancellationToken token = default);

        /// <summary>
        /// Returns actual database version from migration history table
        /// </summary>
        /// <param name="isDowngradeEnabled">Indicates that downgrade is enabled for migrator. Affects how migration history table is analyzed.</param>
        /// <param name="token">Cancellation token</param>
        Task<DbVersion?> GetDbVersionSafeAsync(bool isDowngradeEnabled, CancellationToken token = default);

        /// <summary>
        /// Update actual database version in migration history table
        /// </summary>
        /// <exception cref="MigrationException"></exception>
        Task UpdateCurrentDbVersionAsync(string migrationName, DbVersion version, CancellationToken token = default);

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
}