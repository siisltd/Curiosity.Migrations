using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
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
        public async Task OpenConnectionAsync()
        {
            if (Connection != null && Connection.State != ConnectionState.Closed &&
                Connection.State != ConnectionState.Broken)
                throw new InvalidOperationException("Connection have already opened");

            await TryExecute(async () =>
            {
                var connection = new NpgsqlConnection(ConnectionString);

                await connection.OpenAsync();

                Connection = connection;
            }, MigrationError.CreatingDbError, $"Can not create database {DbName}");
        }

        /// <inheritdoc />
        public DbTransaction BeginTransaction()
        {
            return Connection.BeginTransaction();
        }

        /// <inheritdoc />
        public async Task<DbState> GetDbStateSafeAsync(DbVersion desireDbVersion)
        {
            try
            {
                var result =
                    await ExecuteScalarScriptWithoutInitialCatalogAsync(String.Format(CheckDbExistQueryFormat, DbName))
                        .ConfigureAwait(false);
                if (result == null || (Int32) result != 1)
                {
                    return DbState.NotCreated;
                }

                AssertConnection(Connection);
                var dbVersion = await InternalGetDbVersionAsync()
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
        public async Task CreateDatabaseIfNotExistsAsync()
        {
            await TryExecute(async () =>
            {
                var result =
                    await ExecuteScalarScriptWithoutInitialCatalogAsync(String.Format(CheckDbExistQueryFormat, DbName));
                if (result == null || result is int i && i != 1 || result is bool b && !b)
                {
                    await ExecuteScriptWithoutInitialCatalogAsync(GetCreationDbQuery());
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
        public async Task<bool> CheckIfDatabaseExistsAsync(string databaseName)
        {
            return await TryExecute(async () =>
            {
                var result =
                    await ExecuteScalarScriptWithoutInitialCatalogAsync(String.Format(CheckDbExistQueryFormat, DbName));
                return result != null && (result is int i && i == 1 || result is bool b && b);
            }, MigrationError.Unknown, $"Can not check existence of {DbName} database");
        }

        private void AssertConnection(IDbConnection connection)
        {
            if (connection == null 
                || connection.State == ConnectionState.Closed 
                || connection.State == ConnectionState.Broken)
                throw new InvalidOperationException($"Connection is not opened. Use {nameof(OpenConnectionAsync)}");
        }

        private async Task ExecuteScriptAsync(string connectionString, string script)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await InternalExecuteScriptAsync(connection, script);
            }
        }

        private async Task InternalExecuteScriptAsync(NpgsqlConnection connection, string script)
        {
            var command = connection.CreateCommand();
            command.CommandText = script;
            await command
                .ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task CreateHistoryTableIfNotExistsAsync()
        {
            AssertConnection(Connection);

            var script = $"CREATE TABLE IF NOT EXISTS public.\"{MigrationHistoryTableName}\" "
                         + "( \"version\" varchar(10) NULL )"
                         + @" 
                        WITH ( 
                          OIDS=FALSE 
                        ); ";
            await TryExecute(async () => { await InternalExecuteScriptAsync(Connection as NpgsqlConnection, script); },
                MigrationError.CreatingHistoryTable, $"Can not create database {DbName}");
        }

        /// <inheritdoc />
        public async Task<bool> CheckIfTableExistsAsync(string tableName)
        {
            if (String.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            AssertConnection(Connection);

            return await TryExecute(async () =>
            {
                var checkTableExistenceQuery = $@"SELECT EXISTS (
                                                       SELECT 1
                                                       FROM   information_schema.tables
                                                WHERE  table_schema = '{GetSchemeNameFromConnectionString()}'
                                                AND    table_name = '{tableName}');";
                var result =
                    await InternalExecuteScalarScriptAsync(Connection as NpgsqlConnection, checkTableExistenceQuery);
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
        public async Task<DbVersion?> GetDbVersionSafeAsync()
        {
            try
            {
                return await InternalGetDbVersionAsync();
            }
            catch (Exception)
            {
                return default;
            }
        }

        private async Task<DbVersion?> InternalGetDbVersionAsync()
        {
            var query = $"SELECT * FROM public.\"{_options.MigrationHistoryTableName}\" LIMIT 1;";

            var command = (Connection as NpgsqlConnection).CreateCommand();
            command.CommandText = query;

            using (var reader = await command.ExecuteReaderAsync())
            {
                if (!reader.HasRows) return default(DbVersion?);

                await reader.ReadAsync();
                var stringVersion = reader.GetString(0);

                if (!DbVersion.TryParse(stringVersion, out var version))
                {
                    throw new InvalidOperationException("Cannot get database version.");
                }

                return version;
            }
        }

        /// <inheritdoc />
        public async Task UpdateCurrentDbVersionAsync(DbVersion version)
        {
            AssertConnection(Connection);

            var script = $"DELETE FROM public.\"{_options.MigrationHistoryTableName}\"; "
                         + $"INSERT INTO public.\"{_options.MigrationHistoryTableName}\"("
                         + "version) "
                         + $" VALUES ('{version.ToString()}'); ";

            await TryExecute(async () => { await InternalExecuteScriptAsync(Connection as NpgsqlConnection, script); },
                MigrationError.MigratingError, $"Can not update DB {DbName} version");
        }

        /// <inheritdoc />
        public async Task ExecuteScriptAsync(string script)
        {
            AssertConnection(Connection);

            await TryExecute(async () => { await InternalExecuteScriptAsync(Connection as NpgsqlConnection, script); },
                MigrationError.MigratingError, "Can not execute script");
        }

        /// <inheritdoc />
        public async Task<int> ExecuteNonQueryScriptAsync(string script)
        {
            AssertConnection(Connection);
            return await TryExecute(
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
        public async Task<object> ExecuteScalarScriptAsync(string script)
        {
            AssertConnection(Connection);
            return await TryExecute(async () =>
            {
                var result = await InternalExecuteScalarScriptAsync(Connection as NpgsqlConnection, script);
                return result;
            }, MigrationError.MigratingError, $"Can not execute script");
        }

        private Task<object> InternalExecuteScalarScriptAsync(NpgsqlConnection connection, string query)
        {
            var command = connection.CreateCommand();
            command.CommandText = query;
            return command.ExecuteScalarAsync();
        }

        /// <inheritdoc />
        public async Task ExecuteScriptWithoutInitialCatalogAsync(string script)
        {
            await TryExecute(async () => { await ExecuteScriptAsync(_connectionStringWithoutInitialCatalog, script); },
                MigrationError.MigratingError, "Can not execute script");
        }

        /// <inheritdoc />
        public async Task<object> ExecuteScalarScriptWithoutInitialCatalogAsync(string script)
        {
            return await TryExecute(async () =>
            {
                using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection.OpenAsync();
                    var result = await InternalExecuteScalarScriptAsync(connection, script);

                    return result;
                }
            }, MigrationError.MigratingError, $"Can not execute script");
        }

        /// <inheritdoc />
        public Task CloseConnectionAsync()
        {
            return TryExecute(() =>
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
        private async Task<T> TryExecute<T>(Func<Task<T>> action, MigrationError errorType, string errorMessage)
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
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
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
        /// All catched exceptions will be processed and re-thrown.
        /// </summary>
        /// <param name="action">Code that should be executed and that can cause postgres exceptions.</param>
        /// <param name="errorType">Type for exception that will be thrown if unknown exception occurres.</param>
        /// <param name="errorMessage">Message for exception that will be thrown if unknown exception occurres.</param>
        /// <exception cref="InvalidOperationException">In case when IOE get cought - it will be rethrown.</exception>
        /// <exception cref="MigrationException">Any exception except IOE will be rethrown as MigrationException.</exception>
        private async Task TryExecute(Func<Task> action, MigrationError errorType, string errorMessage)
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
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
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