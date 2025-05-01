using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations.SqlServer;

/// <summary>
/// Facade over connection to a SQL Server database for migration.
/// </summary>
/// <inheritdoc cref="IMigrationConnection"/>
public class SqlServerMigrationConnection : IMigrationConnection
{
    /// <summary>
    /// Connection string without initial catalog, pointing to the default/master database.
    /// </summary>
    private readonly string _connectionStringWithoutInitialCatalog;
    private readonly SqlServerMigrationConnectionOptions _options;
    private readonly MigrationActionHelper _actionHelper;

    /// <summary>
    /// Dictionary with default variables from connection string and DB connection.
    /// </summary>
    /// <remarks>
    /// Key - variable name, value - variable value.
    /// </remarks>
    private readonly Dictionary<string, string> _defaultVariables;

    /// <summary>
    /// Logger for sql queries.
    /// </summary>
    private ILogger? _sqlLogger;

    /// <inheritdoc />
    public string DatabaseName { get; }

    /// <inheritdoc />
    public string ConnectionString { get; }

    /// <summary>
    /// SQL Server connection to database.
    /// </summary>
    public SqlConnection? SqlConnection { get; private set; }

    /// <inheritdoc />
    public DbConnection? Connection => SqlConnection;

    /// <inheritdoc />
    public string MigrationHistoryTableName { get; }

    /// <inheritdoc cref="SqlServerMigrationConnection"/>
    /// <param name="options">Options to connect and manage database.</param>
    public SqlServerMigrationConnection(SqlServerMigrationConnectionOptions options)
    {
        Guard.AssertNotNull(options, nameof(options));

        MigrationHistoryTableName = options.MigrationHistoryTableName;

        var connectionBuilder = new SqlConnectionStringBuilder(options.ConnectionString);
        DatabaseName = connectionBuilder.InitialCatalog ?? throw new ArgumentException($"{nameof(connectionBuilder.InitialCatalog)} can't be empty");
        ConnectionString = connectionBuilder.ConnectionString;

        var tempConnectionBuilder = new SqlConnectionStringBuilder(options.ConnectionString)
        {
            InitialCatalog = options.DefaultDatabase
        };
        _connectionStringWithoutInitialCatalog = tempConnectionBuilder.ConnectionString;

        _options = options;

        _defaultVariables = new Dictionary<string, string>
        {
            [DefaultVariables.User] = connectionBuilder.UserID ?? "unknown",
            [DefaultVariables.DbName] = connectionBuilder.InitialCatalog
        };

        SqlConnection = null!;

        _actionHelper = new MigrationActionHelper(DatabaseName);
    }

    /// <inheritdoc />
    public void UseSqlLogger(ILogger? logger)
    {
        _sqlLogger = logger;
    }

    /// <inheritdoc />
    public Task OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (Connection != null && Connection.State != ConnectionState.Closed &&
            Connection.State != ConnectionState.Broken)
            throw new InvalidOperationException("Connection has already been opened");

        return _actionHelper.TryExecuteAsync(
            async ct =>
            {
                var connection = new SqlConnection(ConnectionString);

                await connection.OpenAsync(ct);

                SqlConnection = connection;
            },
            MigrationErrorCode.CreatingDbError,
            $"Can not open connection to database \"{DatabaseName}\"",
            cancellationToken);
    }

    /// <inheritdoc />
    public DbTransaction BeginTransaction()
    {
        SqlServerGuard.AssertConnection(SqlConnection);

        return SqlConnection!.BeginTransaction();
    }

    /// <inheritdoc />
    public Task CreateDatabaseIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        return _actionHelper.TryExecuteAsync(
            async ct =>
            {
                var isDatabaseExist = await CheckIfDatabaseExistsAsync(DatabaseName, ct);
                if (isDatabaseExist) return;

                var createDbQuery = BuildCreateDatabaseSqlQuery();
                await ExecuteNonQuerySqlWithoutInitialCatalogAsync(
                    createDbQuery,
                    null,
                    ct);
                
                // If isolation options are set, apply them after database creation
                if (_options.AllowSnapshotIsolation || _options.ReadCommittedSnapshot)
                {
                    await ConfigureDatabaseIsolationLevelsAsync(ct);
                }
            },
            MigrationErrorCode.CreatingDbError,
            $"Can not create database \"{DatabaseName}\"",
            cancellationToken);
    }

    private async Task ConfigureDatabaseIsolationLevelsAsync(CancellationToken cancellationToken)
    {
        // We need to open a connection to the newly created database
        await OpenConnectionAsync(cancellationToken);
        
        try
        {
            if (_options.AllowSnapshotIsolation)
            {
                var query = $"ALTER DATABASE [{DatabaseName}] SET ALLOW_SNAPSHOT_ISOLATION ON";
                await ExecuteNonQuerySqlAsync(query, null, cancellationToken);
            }
            
            if (_options.ReadCommittedSnapshot)
            {
                var query = $"ALTER DATABASE [{DatabaseName}] SET READ_COMMITTED_SNAPSHOT ON";
                await ExecuteNonQuerySqlAsync(query, null, cancellationToken);
            }
        }
        finally
        {
            await CloseConnectionAsync();
        }
    }

    /// <inheritdoc />
    public Task<bool> CheckIfDatabaseExistsAsync(
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(databaseName, nameof(databaseName));

        return _actionHelper.TryExecuteAsync(
            async ct =>
            {
                var result =
                    await ExecuteScalarSqlWithoutInitialCatalogAsync(
                        "SELECT COUNT(*) FROM sys.databases WHERE name = @databaseName",
                        new Dictionary<string, object?>
                        {
                            {"@databaseName", databaseName}
                        },
                        ct);
                return Convert.ToInt32(result) > 0;
            },
            MigrationErrorCode.Unknown,
            $"Can not check existence of a \"{databaseName}\" database",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<object?> ExecuteScalarSqlWithoutInitialCatalogAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(sqlQuery, nameof(sqlQuery));

        return _actionHelper.TryExecuteAsync(
            async ct =>
            {
                using var connection = new SqlConnection(_connectionStringWithoutInitialCatalog);
                await connection.OpenAsync(ct);
                var result = await ExecuteScalarSqlInternalAsync(
                    connection,
                    sqlQuery,
                    queryParams,
                    ct);

                return result;
            },
            MigrationErrorCode.MigratingError,
            "Can not execute SQL query",
            cancellationToken);
    }

    private Task<object?> ExecuteScalarSqlInternalAsync(
        SqlConnection connection,
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
        SqlCommand command,
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
        SqlCommand command,
        IReadOnlyDictionary<string, object?> commandParams)
    {
        Guard.AssertNotNull(command, nameof(command));

        foreach (var kvp in commandParams)
        {
            command.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
        }
    }

    private void LogCommand(SqlCommand command)
    {
        if (_sqlLogger?.IsEnabled(LogLevel.Debug) != true) return;

        var builder = new StringBuilder(command.CommandText);
        builder.AppendLine();

        foreach (SqlParameter parameter in command.Parameters)
        {
            builder.AppendLine($"{parameter.ParameterName} = {parameter.Value}");
        }

        _sqlLogger.LogDebug(builder.ToString());
    }

    private Task<int> ExecuteNonQueryInternalAsync(
        SqlConnection connection,
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
        SqlCommand command,
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
            async ct =>
            {
                using var connection = new SqlConnection(_connectionStringWithoutInitialCatalog);
                await connection.OpenAsync(ct);

                var result = await ExecuteNonQueryInternalAsync(
                    connection,
                    sqlQuery,
                    queryParams,
                    ct);

                return result;
            },
            MigrationErrorCode.MigratingError,
            $"Can not execute SQL query without initial catalog",
            cancellationToken);
    }

    private string BuildCreateDatabaseSqlQuery()
    {
        var queryBuilder = new StringBuilder();
        
        // Start with basic CREATE DATABASE statement
        queryBuilder.Append($"CREATE DATABASE [{DatabaseName}]");
        
        // Add collation if specified - must come right after database name
        if (_options.Collation != null)
        {
            queryBuilder.Append($" COLLATE {_options.Collation}");
        }
        queryBuilder.AppendLine();
        
        // Add file specifications only if explicitly provided
        if (_options.DataFilePath != null)
        {
            queryBuilder.AppendLine("ON PRIMARY");
            queryBuilder.AppendLine("(");
            queryBuilder.AppendLine($"    NAME = {DatabaseName}_data,");
            queryBuilder.AppendLine($"    FILENAME = '{_options.DataFilePath}\\{DatabaseName}.mdf'");
            
            if (_options.InitialSize.HasValue)
                queryBuilder.AppendLine($"    ,SIZE = {_options.InitialSize}MB");
            
            if (_options.MaxSize.HasValue)
                queryBuilder.AppendLine($"    ,MAXSIZE = {_options.MaxSize}MB");
            else if (_options.DataFilePath != null)
                queryBuilder.AppendLine("    ,MAXSIZE = UNLIMITED");
            
            if (_options.FileGrowth.HasValue)
                queryBuilder.AppendLine($"    ,FILEGROWTH = {_options.FileGrowth}MB");
            
            queryBuilder.AppendLine(")");
            
            // Add log file spec if log path is provided
            if (_options.LogFilePath != null)
            {
                queryBuilder.AppendLine("LOG ON");
                queryBuilder.AppendLine("(");
                queryBuilder.AppendLine($"    NAME = {DatabaseName}_log,");
                queryBuilder.AppendLine($"    FILENAME = '{_options.LogFilePath}\\{DatabaseName}.ldf'");
                queryBuilder.AppendLine(")");
            }
        }
        
        return queryBuilder.ToString();
    }

    /// <inheritdoc />
    public async Task CreateMigrationHistoryTableIfNotExistsAsync(CancellationToken cancellationToken = default)
    {
        SqlServerGuard.AssertConnection(SqlConnection);
        
        var schema = _options.SchemaName ?? await GetSchemaNameFromConnectionStringAsync(cancellationToken);
        
        await _actionHelper.TryExecuteAsync(
            async ct =>
            {
                // First, ensure the schema exists
                await EnsureSchemaExistsAsync(schema, ct);
                
                var isTableExist = await CheckIfTableExistsAsync(MigrationHistoryTableName, ct);
                if (isTableExist) return;

                var tableScript = GetMigrationHistoryTableScript(schema, MigrationHistoryTableName);
                await ExecuteNonQuerySqlAsync(tableScript, null, ct);
            },
            MigrationErrorCode.CreatingHistoryTable,
            $"Can not create migration history table \"{MigrationHistoryTableName}\"",
            cancellationToken);
    }

    private async Task EnsureSchemaExistsAsync(string schema, CancellationToken cancellationToken)
    {
        if (schema.Equals("dbo", StringComparison.OrdinalIgnoreCase)) 
            return; // dbo schema always exists
        
        var schemaExists = await ExecuteScalarSqlAsync(
            "SELECT COUNT(*) FROM sys.schemas WHERE name = @schemaName",
            new Dictionary<string, object?> { { "@schemaName", schema } },
            cancellationToken);
        
        if (Convert.ToInt32(schemaExists) == 0)
        {
            await ExecuteNonQuerySqlAsync(
                $"CREATE SCHEMA [{schema}]",
                null,
                cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task<bool> CheckIfTableExistsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(tableName, nameof(tableName));
        SqlServerGuard.AssertConnection(SqlConnection);

        return _actionHelper.TryExecuteAsync(
            async ct =>
            {
                var schema = _options.SchemaName ?? await GetSchemaNameFromConnectionStringAsync(ct);
                var result = await ExecuteScalarSqlAsync(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = @tableName",
                    new Dictionary<string, object?>
                    {
                        {"@schema", schema},
                        {"@tableName", tableName}
                    },
                    ct);
                
                return Convert.ToInt32(result) > 0;
            },
            MigrationErrorCode.MigratingError,
            $"Can not check existence of a table \"{tableName}\"",
            cancellationToken);
    }

    private async Task<string> GetSchemaNameFromConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        SqlServerGuard.AssertConnection(SqlConnection);

        // Get the default schema for the current user
        var result = await ExecuteScalarSqlAsync(
            "SELECT SCHEMA_NAME()",
            null,
            cancellationToken);

        return result?.ToString() ?? SqlServerMigrationConnectionOptions.DefaultSchemaName;
    }

    /// <inheritdoc />
    public Task<object?> ExecuteScalarSqlAsync(
        string script,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(script, nameof(script));
        SqlServerGuard.AssertConnection(SqlConnection);

        return _actionHelper.TryExecuteAsync(
            ct => ExecuteScalarSqlInternalAsync(
                SqlConnection!,
                script,
                queryParams,
                ct),
            MigrationErrorCode.MigratingError,
            "Can not execute SQL query",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<MigrationVersion>> GetAppliedMigrationVersionsAsync(
        CancellationToken cancellationToken = default)
    {
        SqlServerGuard.AssertConnection(SqlConnection);

        return await _actionHelper.TryExecuteAsync(
            async ct =>
            {
                var schema = _options.SchemaName ?? await GetSchemaNameFromConnectionStringAsync(ct);

                // First check if the table exists
                var isTableExist = await CheckIfTableExistsAsync(MigrationHistoryTableName, ct);
                if (!isTableExist)
                {
                    return Array.Empty<MigrationVersion>();
                }

                var result = new List<MigrationVersion>();
                using var command = SqlConnection!.CreateCommand();
                command.CommandText = $"SELECT version FROM [{schema}].[{MigrationHistoryTableName}]";

                LogCommand(command);

                using var reader = await command.ExecuteReaderAsync(ct);
                if (!reader.HasRows) return Array.Empty<MigrationVersion>();

                while (await reader.ReadAsync(ct))
                {
                    var stringVersion = reader.GetString(0);

                    if (!MigrationVersion.TryParse(stringVersion, out var version))
                        throw new MigrationException(
                            MigrationErrorCode.MigratingError,
                            $"Incorrect migration version (source value = {stringVersion}).",
                            DatabaseName);

                    result.Add(version);
                }

                return result.OrderBy(x => x).ToArray();
            },
            MigrationErrorCode.MigratingError,
            "Can not get applied migration versions", cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveAppliedMigrationVersionAsync(
        MigrationVersion version,
        string? migrationName,
        CancellationToken cancellationToken = default)
    {
        SqlServerGuard.AssertConnection(SqlConnection);

        return _actionHelper.TryExecuteAsync(
            async ct =>
            {
                var schema = _options.SchemaName ?? await GetSchemaNameFromConnectionStringAsync(ct);
                var query = $"INSERT INTO [{schema}].[{MigrationHistoryTableName}] (name, version, created) " +
                          "VALUES (@migrationName, @version, @createdAt)";
                
                await ExecuteNonQuerySqlAsync(
                    query,
                    new Dictionary<string, object?>
                    {
                        {"@migrationName", migrationName},
                        {"@version", version.ToString()},
                        {"@createdAt", DateTime.UtcNow}
                    },
                    ct);
            },
            MigrationErrorCode.MigratingError,
            $"Can not save info about applied migration \"{version}\"",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAppliedMigrationVersionAsync(MigrationVersion version, CancellationToken cancellationToken = default)
    {
        SqlServerGuard.AssertConnection(SqlConnection);

        return _actionHelper.TryExecuteAsync(
            async ct =>
            {
                var schema = _options.SchemaName ?? await GetSchemaNameFromConnectionStringAsync(ct);
                var query = $"DELETE FROM [{schema}].[{MigrationHistoryTableName}] WHERE version = @version";
                
                await ExecuteNonQuerySqlAsync(
                    query,
                    new Dictionary<string, object?>
                    {
                        {"@version", version.ToString()}
                    },
                    ct);
            },
            MigrationErrorCode.MigratingError,
            $"Can not delete info about applied migration \"{version}\"",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<int> ExecuteNonQuerySqlAsync(
        string sqlQuery,
        IReadOnlyDictionary<string, object?>? queryParams,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotEmpty(sqlQuery, nameof(sqlQuery));
        SqlServerGuard.AssertConnection(SqlConnection);

        return _actionHelper.TryExecuteAsync(
            ct => ExecuteNonQueryInternalAsync(
                SqlConnection!,
                sqlQuery,
                queryParams,
                ct),
            MigrationErrorCode.MigratingError,
            "Can not execute non-query SQL",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task CloseConnectionAsync()
    {
        return _actionHelper.TryExecuteAsync(
            _ =>
            {
                if (SqlConnection == null) return Task.CompletedTask;
                
                SqlConnection.Close();
                SqlConnection.Dispose();

                SqlConnection = null;
                
                return Task.CompletedTask;
            },
            MigrationErrorCode.Unknown,
            "Can not close database connection");
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetDefaultVariables()
    {
        return _defaultVariables;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SqlConnection?.Dispose();
    }

    private string GetMigrationHistoryTableScript(string schema, string tableName)
    {
        return $@"
CREATE TABLE [{schema}].[{tableName}] (
    id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    created DATETIME NOT NULL DEFAULT GETUTCDATE(),
    name NVARCHAR(MAX),
    version NVARCHAR(50) NOT NULL
);

CREATE UNIQUE INDEX UX_{tableName}_version ON [{schema}].[{tableName}] (version);";
    }
} 