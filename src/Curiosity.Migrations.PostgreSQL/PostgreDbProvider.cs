using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Curiosity.Migrations.PostgreSQL
{
    /// <summary>
    /// Provide access to Postgre database
    /// </summary>
    public class PostgreDbProvider : IDbProvider
    {
        private const string CheckDbExistQueryFormat = "SELECT 1 AS result FROM pg_database WHERE datname='{0}'";

        /// <summary>
        /// 'Postgres' database is garanteed to exist, need to create own db
        /// </summary>
        /// <remarks>
        /// Can not open connection without specified database name
        /// </remarks>
        private const string PostgreDefaultDatabase = "postgres";

        /// <inheritdoc />
        public string DbName { get; }

        /// <inheritdoc />
        public string ConnectionString { get; }

        /// <inheritdoc />
        public DbConnection Connection { get; private set; }

        /// <inheritdoc />
        public string MigrationHistoryTableName { get; }

        private readonly string _connectionStringWithoutInitialCatalog;

        private readonly PostgreDbProviderOptions _options;

        /// <summary>
        /// Dictionary with default variables from connection string and DB connection
        /// </summary>
        /// <remarks>
        /// Key - variable name, value - variable value
        /// </remarks>
        private readonly Dictionary<string, string> _defaultVariables;

        /// <summary>
        /// Provide access to Postgre database
        /// </summary>
        /// <param name="options">Options to connect and manage database</param>
        public PostgreDbProvider(PostgreDbProviderOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.ConnectionString))
                throw new ArgumentNullException(nameof(options.ConnectionString));

            MigrationHistoryTableName = options.MigrationHistoryTableName;
            var connectionBuilder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
            DbName = connectionBuilder.Database;
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
                [DefaultVariables.User] = tempConnectionBuilder.Username,
                [DefaultVariables.DbName] = tempConnectionBuilder.Database
            };
        }

        /// <inheritdoc />
        public async Task OpenConnectionAsync(CancellationToken token = default)
        {
            if (Connection != null && Connection.State != ConnectionState.Closed &&
                Connection.State != ConnectionState.Broken)
                throw new InvalidOperationException("Connection have already opened");

            await TryExecuteAsync(async () =>
            {
                var connection = new NpgsqlConnection(ConnectionString);

                await connection.OpenAsync(token);

                Connection = connection;
            }, MigrationError.CreatingDbError, $"Can not create database {DbName}");
        }

        /// <inheritdoc />
        public DbTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }

        /// <inheritdoc />
        public async Task<DbState> GetDbStateSafeAsync(DbVersion desireDbVersion, CancellationToken token = default)
        {
            try
            {
                var result =
                    await ExecuteScalarScriptWithoutInitialCatalogAsync(String.Format(CheckDbExistQueryFormat, DbName), token)
                        .ConfigureAwait(false);
                if (result == null || (Int32) result != 1)
                {
                    return DbState.NotCreated;
                }

                AssertConnection(Connection);
                var dbVersion = await GetDbVersionAsync(token)
                    .ConfigureAwait(false);
                if (dbVersion == null) return DbState.Outdated;
                if (dbVersion.Value == desireDbVersion) return DbState.Ok;
                return dbVersion.Value < desireDbVersion
                    ? DbState.Outdated
                    : DbState.Newer;
            }
            catch (Exception)
            {
                return DbState.Unknown;
            }
        }

        /// <inheritdoc />
        public Task CreateDatabaseIfNotExistsAsync(CancellationToken token = default)
        {
            return TryExecuteAsync(async () =>
            {
                var result =
                    await ExecuteScalarScriptWithoutInitialCatalogAsync(String.Format(CheckDbExistQueryFormat, DbName), token);
                if (result == null || result is int i && i != 1 || result is bool b && !b)
                {
                    await ExecuteScriptWithoutInitialCatalogAsync(GetCreationDbQuery(), token);
                }

            }, MigrationError.CreatingDbError, $"Can not create database {DbName}");
        }

        private string GetCreationDbQuery()
        {
            var queryBuilder = new StringBuilder();
            queryBuilder.Append($"CREATE DATABASE \"{DbName}\" ");
            if (String.IsNullOrWhiteSpace(_options.DatabaseEncoding)
                && String.IsNullOrWhiteSpace(_options.LC_COLLATE)
                && String.IsNullOrWhiteSpace(_options.LC_CTYPE)
                && !_options.ConnectionLimit.HasValue
                && String.IsNullOrWhiteSpace(_options.TableSpace)
                && String.IsNullOrWhiteSpace(_options.Template))
            {
                return queryBuilder.ToString();
            }

            queryBuilder.AppendLine("WITH ");

            if (!String.IsNullOrWhiteSpace(_options.DatabaseEncoding))
            {
                queryBuilder.AppendLine($"ENCODING = '{_options.DatabaseEncoding}'");
            }

            if (!String.IsNullOrWhiteSpace(_options.LC_COLLATE))
            {
                queryBuilder.AppendLine($"LC_COLLATE = '{_options.LC_COLLATE}'");
            }

            if (!String.IsNullOrWhiteSpace(_options.LC_CTYPE))
            {
                queryBuilder.AppendLine($"LC_CTYPE = '{_options.LC_CTYPE}'");
            }

            if (!String.IsNullOrWhiteSpace(_options.Template))
            {
                queryBuilder.AppendLine($"TEMPLATE = '{_options.Template}'");
            }

            if (!String.IsNullOrWhiteSpace(_options.TableSpace))
            {
                queryBuilder.AppendLine($"TABLESPACE = '{_options.TableSpace}'");
            }

            if (_options.ConnectionLimit.HasValue)
            {
                queryBuilder.AppendLine($"CONNECTION LIMIT = {_options.ConnectionLimit.Value}");
            }

            return queryBuilder.ToString();
        }

        /// <inheritdoc />
        public Task<bool> CheckIfDatabaseExistsAsync(string databaseName, CancellationToken token = default)
        {
            return TryExecuteAsync(async () =>
            {
                var result =
                    await ExecuteScalarScriptWithoutInitialCatalogAsync(String.Format(CheckDbExistQueryFormat, databaseName), token);
                return result != null && (result is int i && i == 1 || result is bool b && b);
            }, MigrationError.Unknown, $"Can not check existence of {databaseName} database");
        }

        private void AssertConnection(IDbConnection connection)
        {
            if (connection == null 
                || connection.State == ConnectionState.Closed 
                || connection.State == ConnectionState.Broken)
                throw new InvalidOperationException($"Connection is not opened. Use {nameof(OpenConnectionAsync)}");
        }

        private async Task ExecuteScriptAsync(string connectionString, string script, CancellationToken token = default)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync(token);
                await InternalExecuteScriptAsync(connection, script, token);
            }
        }

        private async Task InternalExecuteScriptAsync(NpgsqlConnection connection, string script, CancellationToken token = default)
        {
            var command = connection.CreateCommand();
            command.CommandText = script;
            await command.ExecuteNonQueryAsync(token);
        }

        /// <inheritdoc />
        public async Task CreateHistoryTableIfNotExistsAsync(CancellationToken token = default)
        {
            AssertConnection(Connection);

            var script = $"CREATE TABLE IF NOT EXISTS public.\"{MigrationHistoryTableName}\" " +
                         $"(id bigserial NOT NULL CONSTRAINT \"{MigrationHistoryTableName}_pkey\" PRIMARY KEY, " +
                         "created timestamp default timezone('UTC'::text, now()) NOT NULL, " +
                         "name text, " +
                         "version varchar(10))" +
                         @" 
                        WITH ( 
                          OIDS=FALSE 
                        ); " +

                         $"ALTER TABLE public.\"{MigrationHistoryTableName}\" OWNER TO {_defaultVariables[DefaultVariables.User]};";
            
            await TryExecuteAsync(
                async () => { await InternalExecuteScriptAsync(Connection as NpgsqlConnection, script, token); },
                MigrationError.CreatingHistoryTable,
                $"Can not create history table \"{MigrationHistoryTableName}\" in database {DbName}");
            
        }

        /// <inheritdoc />
        public Task<bool> CheckIfTableExistsAsync(string tableName, CancellationToken token = default)
        {
            if (String.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            AssertConnection(Connection);

            return TryExecuteAsync(async () =>
            {
                var checkTableExistenceQuery = $@"SELECT EXISTS (
                                                       SELECT 1
                                                       FROM   information_schema.tables
                                                WHERE  table_schema = '{GetSchemeNameFromConnectionString()}'
                                                AND    table_name = '{tableName}');";
                var result =
                    await InternalExecuteScalarScriptAsync(Connection as NpgsqlConnection, checkTableExistenceQuery, token);
                return result != null && (result is int i && i == 1 || result is bool b && b);
            }, MigrationError.Unknown, $"Can not check existence of {tableName} table");
        }

        private string GetSchemeNameFromConnectionString()
        {
            const string schemeParseRegexPattern = "initial schema ?= ?(.+)";
            var regex = new Regex(schemeParseRegexPattern);
            var matches = regex.Match(ConnectionString);

            return matches.Success && matches.Groups.Count > 1 && matches.Groups[1].Success
                ? matches.Groups[1].Value
                : "public";
        }

        /// <inheritdoc />
        public async Task<DbVersion?> GetDbVersionSafeAsync(CancellationToken token = default)
        {
            try
            {
                return await GetDbVersionAsync(token);
            }
            catch (Exception)
            {
                return default;
            }
        }

        /// <inheritdoc />
        public async Task<DbVersion?> GetDbVersionAsync(CancellationToken token = default)
        {
            var query = $"SELECT version FROM public.\"{_options.MigrationHistoryTableName}\" ORDER BY created DESC LIMIT 1;";

            var command = (Connection as NpgsqlConnection).CreateCommand();
            command.CommandText = query;

            try
            {
                using (var reader = await command.ExecuteReaderAsync(token))
                {
                    if (!reader.HasRows) return default;

                    await reader.ReadAsync(token);
                    var stringVersion = reader.GetString(0);

                    if (!DbVersion.TryParse(stringVersion, out var version))
                    {
                        throw new InvalidOperationException("Cannot get database version.");
                    }

                    return version;
                }
            }
            catch (PostgresException e)
            {
                // Migration table does not exist.
                if (e.SqlState == "42P01")
                    return default;

                throw;
            }
        }

        /// <inheritdoc />
        public Task UpdateCurrentDbVersionAsync(string migrationName, DbVersion version, CancellationToken token = default)
        {
            AssertConnection(Connection);

            var script = $"INSERT INTO public.\"{_options.MigrationHistoryTableName}\" "
                         + "(name, version) "
                         + $"VALUES ('{migrationName}', '{version.ToString()}');";

            return TryExecuteAsync(async () => { await InternalExecuteScriptAsync(Connection as NpgsqlConnection, script, token); },
                MigrationError.MigratingError, $"Can not update DB {DbName} version");
        }

        /// <inheritdoc />
        public Task ExecuteScriptAsync(string script, CancellationToken token = default)
        {
            AssertConnection(Connection);

            return TryExecuteAsync(async () => { await InternalExecuteScriptAsync(Connection as NpgsqlConnection, script, token); },
                MigrationError.MigratingError, "Can not execute script");
        }

        /// <inheritdoc />
        public Task<int> ExecuteNonQueryScriptAsync(string script, CancellationToken token = default)
        {
            AssertConnection(Connection);
            return TryExecuteAsync(
                async () =>
                {
                    var result = await InternalExecuteNonQueryScriptAsync(Connection as NpgsqlConnection, script);
                    return result;
                },
                MigrationError.MigratingError, "Can not execute non-query script");
        }

        private Task<int> InternalExecuteNonQueryScriptAsync(NpgsqlConnection connection, string query)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public  Task<object> ExecuteScalarScriptAsync(string script, CancellationToken token = default)
        {
            AssertConnection(Connection);
            return TryExecuteAsync(async () =>
            {
                var result = await InternalExecuteScalarScriptAsync(Connection as NpgsqlConnection, script, token);
                return result;
            }, MigrationError.MigratingError, $"Can not execute script");
        }

        private Task<object> InternalExecuteScalarScriptAsync(NpgsqlConnection connection, string query, CancellationToken token = default)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteScalarAsync(token);
        }

        /// <inheritdoc />
        public Task ExecuteScriptWithoutInitialCatalogAsync(string script, CancellationToken token = default)
        {
            return TryExecuteAsync(async () => { await ExecuteScriptAsync(_connectionStringWithoutInitialCatalog, script, token); },
                MigrationError.MigratingError, "Can not execute script");
        }

        /// <inheritdoc />
        public Task<object> ExecuteScalarScriptWithoutInitialCatalogAsync(string script, CancellationToken token = default)
        {
            return TryExecuteAsync(async () =>
            {
                using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection.OpenAsync(token);
                    var result = await InternalExecuteScalarScriptAsync(connection, script, token);

                    return result;
                }
            }, MigrationError.MigratingError, $"Can not execute script");
        }

        /// <inheritdoc />
        public Task CloseConnectionAsync()
        {
            return TryExecuteAsync(() =>
            {
                if (Connection.State == ConnectionState.Closed) return Task.CompletedTask;

                Connection.Close();
                Connection = null;

                return Task.CompletedTask;
            }, MigrationError.MigratingError, "Can not execute script");
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

        /// <summary>
        /// Try to execute passed function catching common postgres exceptions and return result.
        /// All catched exceptions will be processed and re-thrown.
        /// </summary>
        /// <param name="action">Code that should be executed and that can cause postgres exceptions.</param>
        /// <param name="errorType">Type for exception that will be thrown if unknown exception occurres.</param>
        /// <param name="errorMessage">Message for exception that will be thrown if unknown exception occurres.</param>
        /// <typeparam name="T">Type of returned result.</typeparam>
        /// <returns>Result of invoking passed function.</returns>
        /// <exception cref="InvalidOperationException">In case when IOE get cought - it will be rethrown.</exception>
        /// <exception cref="MigrationException">Any exception except IOE will be rethrown as MigrationException.</exception>
        private async Task<T> TryExecuteAsync<T>(Func<Task<T>> action, MigrationError errorType, string errorMessage)
        {
            try
            {
                return await action.Invoke();
            }
            catch (PostgresException e)
                when (e.SqlState.StartsWith("08")
                      || e.SqlState == "3D000"
                      || e.SqlState == "3F000")
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (PostgresException e)
                when (e.SqlState.StartsWith("28")
                      || e.SqlState == "0P000"
                      || e.SqlState == "42501"
                      || e.SqlState == "42000")
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.MigratingError, $"Error occured while migrating DB {DbName}", e);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MigrationException(errorType, errorMessage, e);
            }
        }

        /// <summary>
        /// Try to execute passed function catching common postgres exceptions.
        /// All caught exceptions will be processed and re-thrown.
        /// </summary>
        /// <param name="action">Code that should be executed and that can cause postgres exceptions.</param>
        /// <param name="errorType">Type for exception that will be thrown if unknown exception occurres.</param>
        /// <param name="errorMessage">Message for exception that will be thrown if unknown exception occurres.</param>
        /// <exception cref="InvalidOperationException">In case when IOE get caught - it will be rethrown.</exception>
        /// <exception cref="MigrationException">Any exception except IOE will be rethrown as MigrationException.</exception>
        private async Task TryExecuteAsync(Func<Task> action, MigrationError errorType, string errorMessage)
        {
            try
            {
                await action.Invoke();
            }
            catch (PostgresException e)
                when (e.SqlState.StartsWith("08")
                      || e.SqlState == "3D000"
                      || e.SqlState == "3F000")
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (PostgresException e)
                when (e.SqlState.StartsWith("28")
                      || e.SqlState == "0P000"
                      || e.SqlState == "42501"
                      || e.SqlState == "42000")
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.MigratingError, $"Error occured while migrating DB {DbName}", e);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MigrationException(errorType, errorMessage, e);
            }
        }
    }
}