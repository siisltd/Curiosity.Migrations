using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Curiosity.Migrations.PostgreSQL;

/// <summary>
/// Facade over connection to a PostgreSQL database for migration.
/// </summary>
/// <inheritdoc cref="IMigrationConnection"/>
public class PostgresMigrationConnection : IMigrationConnection
{
    /// <summary>
    /// 'Postgres' database is guaranteed to exist, needs to create own database.
    /// </summary>
    /// <remarks>
    /// Can not open connection without specified database name
    /// </remarks>
    private const string PostgreDefaultDatabase = "postgres";

    /// <summary>
    /// Connection string without initial catalog, eg. without target database name.
    /// This connection leads to a default database (<see cref="PostgreDefaultDatabase"/>).
    /// </summary>
    private readonly string _connectionStringWithoutInitialCatalog;
    private readonly PostgresMigrationConnectionOptions _options;
    private readonly MigrationActionHelper _actionHelper;

    /// <summary>
    /// Dictionary with default variables from connection string and DB connection
    /// </summary>
    /// <remarks>
    /// Key - variable name, value - variable value
    /// </remarks>
    private readonly Dictionary<string, string> _defaultVariables;

    /// <summary>
    /// Logger for sql queries
    /// </summary>
    private ILogger? _sqLogger;

    /// <inheritdoc />
    public string DatabaseName { get; }

    /// <inheritdoc />
    public string ConnectionString { get; }

    /// <summary>
    /// Npgsql connection to database.
    /// </summary>
    public NpgsqlConnection? NpgsqlConnection { get; private set;  }

    /// <inheritdoc />
    public DbConnection? Connection => NpgsqlConnection;

    /// <inheritdoc />
    public string MigrationHistoryTableName { get; }

    /// <summary>
    /// Provide access to Postgre database
    /// </summary>
    /// <param name="options">Options to connect and manage database</param>
    public PostgresMigrationConnection(PostgresMigrationConnectionOptions options)
    {
        Guard.AssertNotNull(options, nameof(options));

        MigrationHistoryTableName = options.MigrationHistoryTableName;

        var connectionBuilder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
        DatabaseName = connectionBuilder.Database ?? throw new ArgumentException($"{nameof(connectionBuilder.Database)} can't be empty");
        ConnectionString = connectionBuilder.ConnectionString;

        var tempConnectionBuilder =
            new NpgsqlConnectionStringBuilder(options.ConnectionString)
            {
                Database = PostgreDefaultDatabase
            };
        _connectionStringWithoutInitialCatalog = tempConnectionBuilder.ConnectionString;

        _options = options;

        _defaultVariables = new Dictionary<string, string>
        {
            [DefaultVariables.User] = tempConnectionBuilder.Username!,
            [DefaultVariables.DbName] = tempConnectionBuilder.Database
        };

        NpgsqlConnection = null!;

        _actionHelper = new MigrationActionHelper(DatabaseName);
    }

    /// <inheritdoc />
    public void UseSqlLogger(ILogger? logger)
    {
        _sqLogger = logger;
    }

    /// <inheritdoc />
    public Task OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (Connection != null && Connection.State != ConnectionState.Closed &&
            Connection.State != ConnectionState.Broken)
            throw new InvalidOperationException("Connection have been already opened");

        return _actionHelper.TryExecuteAsync(
            async () =>
            {
                var connection = new NpgsqlConnection(ConnectionString);

                await connection.OpenAsync(cancellationToken);

                NpgsqlConnection = connection;
            },
            MigrationErrorCode.CreatingDbError,
            $"Can not open connection to database \"{DatabaseName}\"");
    }

    /// <inheritdoc />
    public DbTransaction BeginTransaction()
    {
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        return NpgsqlConnection!.BeginTransaction();
    }

    /// <inheritdoc />
    public Task CreateDatabaseIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        return _actionHelper.TryExecuteAsync(
            async () =>
            {
                var isDatabaseExist = await CheckIfDatabaseExistsAsync(DatabaseName, cancellationToken);
                if (isDatabaseExist) return;

                var createDbQuery = BuildCreateDatabaseSqlQuery();
                await ExecuteNonQuerySqlWithoutInitialCatalogAsync(
                    createDbQuery,
                    null,
                    cancellationToken);
            },
            MigrationErrorCode.CreatingDbError,
            $"Can not create database \"{DatabaseName}\"");
    }

    /// <inheritdoc />
    public Task<bool> CheckIfDatabaseExistsAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(databaseName, nameof(databaseName));

        return _actionHelper.TryExecuteAsync(
            async () =>
            {
                var result =
                    await ExecuteScalarSqlWithoutInitialCatalogAsync(
                        "SELECT 1 AS result FROM pg_database WHERE datname=@databaseName",
                        new Dictionary<string, object?>
                        {
                            {"@databaseName", databaseName}
                        },
                        cancellationToken);
                return result is 1 or true;
            },
            MigrationErrorCode.Unknown,
            $"Can not check existence of a \"{databaseName}\" database");
    }

    /// <inheritdoc />
    public Task<object?> ExecuteScalarSqlWithoutInitialCatalogAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(sqlQuery, nameof(sqlQuery));

        return _actionHelper.TryExecuteAsync(
            async () =>
            {
                await using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection.OpenAsync(cancellationToken);
                    var result = await ExecuteScalarSqlInternalAsync(
                        connection,
                        sqlQuery,
                        queryParams,
                        cancellationToken);

                    return result;
                }
            },
            MigrationErrorCode.MigratingError,
            "Can not execute SQl query");
    }

    private Task<object?> ExecuteScalarSqlInternalAsync(
        NpgsqlConnection connection,
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotNull(connection, nameof(connection));
        Guard.AssertNotEmpty(sqlQuery, nameof(sqlQuery));

        var command = connection.CreateCommand();
        command.CommandText = sqlQuery;

        return ExecuteScalarCommandInternalAsync(
            command,
            queryParams,
            cancellationToken);
    }

    private Task<object?> ExecuteScalarCommandInternalAsync(
        NpgsqlCommand command,
        IReadOnlyDictionary<string, object?>? commandParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotNull(command, nameof(command));

        if (commandParams != null)
        {
            AddCommandParameters(command, commandParams);
        }

        LogCommand(command);

        return command.ExecuteScalarAsync(cancellationToken);
    }

    private static void AddCommandParameters(
        NpgsqlCommand command,
        IReadOnlyDictionary<string, object?> commandParams)
    {
        Guard.AssertNotNull(command, nameof(command));

        foreach (var kvp in commandParams)
        {
            command.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
        }
    }

    /// <summary>
    /// Logs command's query and params.
    /// </summary>
    private void LogCommand(NpgsqlCommand command)
    {
        Guard.AssertNotNull(command, nameof(command));

        if (_sqLogger == null) return;

        if (command.Parameters.Count == 0)
        {
            _sqLogger.LogInformation($"Executed SQL: {Environment.NewLine}{command.CommandText}");
        }
        else
        {
            var paramsToLog = command.Parameters.Select(x => $"Name=\"{x.ParameterName}\", Value=\"{x.Value}\"");

            _sqLogger.LogInformation(
                $"Executed SQL: {Environment.NewLine}{command.CommandText}{Environment.NewLine}  Parameters:{Environment.NewLine}{String.Join(";", paramsToLog)}");
        }
    }

    private Task<int> ExecuteNonQueryInternalAsync(
        NpgsqlConnection connection,
        string commandText,
        IReadOnlyDictionary<string, object?>? commandParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotNull(connection, nameof(connection));
        Guard.AssertNotEmpty(commandText, nameof(commandText));

        var command = connection.CreateCommand();
        command.CommandText = commandText;

        return ExecuteNonQueryCommandInternalAsync(
            command,
            commandParams,
            cancellationToken);
    }

    private Task<int> ExecuteNonQueryCommandInternalAsync(
        NpgsqlCommand command,
        IReadOnlyDictionary<string, object?>? commandParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotNull(command, nameof(command));

        if (commandParams != null)
        {
            AddCommandParameters(command, commandParams);
        }

        LogCommand(command);

        return command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> ExecuteNonQuerySqlWithoutInitialCatalogAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(sqlQuery, nameof(sqlQuery));

        return _actionHelper.TryExecuteAsync(
            async () =>
            {
                await using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection.OpenAsync(cancellationToken);
                    var result = await ExecuteNonQueryInternalAsync(
                        connection,
                        sqlQuery,
                        queryParams,
                        cancellationToken);

                    return result;
                }
            },
            MigrationErrorCode.MigratingError,
            "Can not execute SQl query");
    }

    /// <summary>
    /// Returns SQL query to create a database.
    /// </summary>
    /// <remarks>
    /// PostgreSQL doesn't allow CREATE DATABASE parametrization.
    /// </remarks>
    private string BuildCreateDatabaseSqlQuery()
    {
        var queryBuilder = new StringBuilder();

        queryBuilder.Append($"CREATE DATABASE \"{DatabaseName}\" ");
        
        var wasAnyOptionAdded = false;

        if (!String.IsNullOrWhiteSpace(_options.DatabaseEncoding))
        {
            AppendLineWithOption(
                queryBuilder,
                $"ENCODING = '{_options.DatabaseEncoding}'",
                ref wasAnyOptionAdded);
        }

        if (!String.IsNullOrWhiteSpace(_options.LC_COLLATE))
        {
            AppendLineWithOption(
                queryBuilder,
                $"LC_COLLATE = '{_options.LC_COLLATE!}'",
                ref wasAnyOptionAdded);
        }

        if (!String.IsNullOrWhiteSpace(_options.LC_CTYPE))
        {
            AppendLineWithOption(
                queryBuilder,
                $"LC_CTYPE = '{_options.LC_CTYPE!}'",
                ref wasAnyOptionAdded);
        }

        if (!String.IsNullOrWhiteSpace(_options.Template))
        {
            AppendLineWithOption(
                queryBuilder,
                $"TEMPLATE = '{_options.Template!}'",
                ref wasAnyOptionAdded);
        }

        if (!String.IsNullOrWhiteSpace(_options.TableSpace))
        {
            AppendLineWithOption(
                queryBuilder,
                $"TABLESPACE = '{_options.TableSpace!}'",
                ref wasAnyOptionAdded);
        }

        if (_options.ConnectionLimit.HasValue)
        {
            AppendLineWithOption(
                queryBuilder,
                $"CONNECTION LIMIT = {_options.ConnectionLimit.Value}",
                ref wasAnyOptionAdded);
        }

        return queryBuilder.ToString();
    }

    /// <summary>
    /// Safely appends query builder with option for a database creation.
    /// </summary>
    private void AppendLineWithOption(
        StringBuilder queryBuilder,
        string option,
        ref bool wasAnyOptionAdded)
    {
        Guard.AssertNotNull(queryBuilder, nameof(queryBuilder));
        Guard.AssertNotEmpty(option, nameof(option));

        if (!wasAnyOptionAdded)
        {
            queryBuilder.AppendLine("WITH ");
            wasAnyOptionAdded = true;
        }

        queryBuilder.AppendLine(option);
    }

    /// <inheritdoc />
    /// <remarks>
    /// PostgreSQL doesn't allow CREATE TABLE parametrization.
    /// </remarks>
    public async Task CreateMigrationHistoryTableIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        // create table
        var queryFormat = @"
CREATE TABLE IF NOT EXISTS {0} (

    id      BIGSERIAL NOT NULL PRIMARY KEY,
    created TIMESTAMP NOT NULL DEFAULT timezone('UTC'::text, now()),
    name    TEXT,
    version TEXT      NOT NULL UNIQUE
)
WITH ( 
  OIDS=FALSE 
);

ALTER TABLE {0} OWNER TO {1};

CREATE UNIQUE INDEX IF NOT EXISTS uix_{0}_version ON {0} (version);
";

        var query = String.Format(queryFormat, MigrationHistoryTableName, _defaultVariables[DefaultVariables.User]);

        await _actionHelper.TryExecuteAsync(
            () => ExecuteNonQueryInternalAsync(
                NpgsqlConnection!,
                query,
                null,
                cancellationToken),
            MigrationErrorCode.CreatingHistoryTable,
            $"Can not create history table \"{MigrationHistoryTableName}\" in database \"{DatabaseName}\"");
    }

    /// <inheritdoc />
    public Task<bool> CheckIfTableExistsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(tableName, nameof(tableName));
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        return _actionHelper.TryExecuteAsync(
            async () =>
            {
                var checkTableExistenceQuery = @"
SELECT EXISTS (
    SELECT 1
    FROM   information_schema.tables
    WHERE  table_schema = @tableScheme
    AND    table_name = @tableName
);";

                var result = await ExecuteScalarSqlInternalAsync(
                        NpgsqlConnection!,
                        checkTableExistenceQuery,
                        new Dictionary<string, object?>
                        {
                            {"@tableScheme", GetSchemeNameFromConnectionString()},
                            {"@tableName", tableName}
                        },
                        cancellationToken);
                return result is 1 or true;
            },
            MigrationErrorCode.Unknown,
            $"Can not check existence of \"{tableName}\" table");
    }

    private string GetSchemeNameFromConnectionString()
    {
        const string schemeParseRegexPattern = "initial schema ?= ?(.+)";
        var regex = new Regex(schemeParseRegexPattern);
        var matches = regex.Match(ConnectionString);

        return matches is { Success: true, Groups.Count: > 1 } && matches.Groups[1].Success
            ? matches.Groups[1].Value
            : "public";
    }

    /// <inheritdoc />
    public Task<object?> ExecuteScalarSqlAsync(
        string script,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(script, nameof(script));
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        return _actionHelper.TryExecuteAsync(
            () => ExecuteScalarSqlInternalAsync(
                NpgsqlConnection!,
                script,
                queryParams,
                cancellationToken),
            MigrationErrorCode.MigratingError,
            "Can not execute script");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<DbVersion>> GetAppliedMigrationVersionsAsync(CancellationToken cancellationToken = default)
    {
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        // actual version is made by last migration because downgrade can decrease version
        var query = $"SELECT version FROM \"{_options.MigrationHistoryTableName}\"";

        var command = NpgsqlConnection!.CreateCommand();
        command.CommandText = query;

        return await _actionHelper.TryExecuteAsync(
            async () =>
            {
                try
                {
                    LogCommand(command);

                    var appliedMigrations = new List<DbVersion>();

                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        if (!reader.HasRows) return Array.Empty<DbVersion>();

                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var stringVersion = reader.GetString(0);

                            if (!DbVersion.TryParse(stringVersion, out var version))
                                throw new InvalidOperationException($"Incorrect migration version (source value = {stringVersion}).");

                            appliedMigrations.Add(version);
                        }

                        return appliedMigrations.OrderBy(x => x).ToArray();
                    }
                }
                catch (PostgresException e)
                {
                    // Migration table does not exist.
                    if (e.SqlState == "42P01")
                        return Array.Empty<DbVersion>();

                    throw;
                }
            },
            MigrationErrorCode.MigratingError,
            "Can't fetch applied migrations from history table");
    }
    /// <inheritdoc />
    public Task SaveAppliedMigrationVersionAsync(
        DbVersion version,
        string? migrationName,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(migrationName, nameof(migrationName));
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        var sqlFormat = @"
INSERT INTO {0} (name, version)
VALUES (@migrationName, @version)";
        var sql = String.Format(sqlFormat, MigrationHistoryTableName);

        return _actionHelper.TryExecuteAsync(
            () => ExecuteNonQueryInternalAsync(
                NpgsqlConnection!,
                sql,
                new Dictionary<string, object?>
                {
                    {"@migrationName", migrationName},
                    {"@version", version.ToString()}
                },
                cancellationToken),
            MigrationErrorCode.MigratingError,
            $"Can not save applied migration version to database \"{DatabaseName}\" (version = {version})");
    }

    /// <inheritdoc />
    public Task DeleteAppliedMigrationVersionAsync(DbVersion version, CancellationToken cancellationToken = default)
    {
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        var sqlFormat = @"DELETE FROM {0} WHERE version = @version";
        var sql = String.Format(sqlFormat, MigrationHistoryTableName);

        return _actionHelper.TryExecuteAsync(
            () => ExecuteNonQueryInternalAsync(
                NpgsqlConnection!,
                sql,
                new Dictionary<string, object?>
                {
                    {"@version", version.ToString()}
                },
                cancellationToken),
            MigrationErrorCode.MigratingError,
            $"Can not delete applied migration version from database \"{DatabaseName}\" (version = {version})");
    }

    /// <inheritdoc />
    public Task<int> ExecuteNonQuerySqlAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(sqlQuery, nameof(sqlQuery));
        PostgresqlGuard.AssertConnection(NpgsqlConnection);

        return _actionHelper.TryExecuteAsync(
            () => ExecuteNonQueryInternalAsync(
                NpgsqlConnection!,
                sqlQuery,
                queryParams,
                cancellationToken),
            MigrationErrorCode.MigratingError,
            "Can not execute script");
    }

    /// <inheritdoc />
    public Task CloseConnectionAsync()
    {
        return _actionHelper.TryExecuteAsync(
            () =>
            {
                if (NpgsqlConnection == null) return Task.CompletedTask;
                if (NpgsqlConnection.State == ConnectionState.Closed) return Task.CompletedTask;

                NpgsqlConnection.Close();
                NpgsqlConnection = null;

                return Task.CompletedTask;
            },
            MigrationErrorCode.MigratingError,
            "Can not close connection to a database");
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetDefaultVariables()
    {
        return _defaultVariables;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Connection?.Dispose();
    }
}
