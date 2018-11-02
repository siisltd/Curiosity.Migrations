using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;

namespace Marvin.Migrations.PostgreSQL
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

        public string DbName { get; }

        private readonly string _connectionString;
        private readonly string _connectionStringWithoutInitialCatalog;

        private NpgsqlConnection _connection;

        private readonly PostgreDbProviderOptions _options;
        
        /// <summary>
        /// Provide access to Postgre database
        /// </summary>
        /// <param name="options">Options to connect and manage database</param>
        public PostgreDbProvider(PostgreDbProviderOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrEmpty(options.ConnectionString)) throw new ArgumentNullException(nameof(options.ConnectionString));


            var connectionBuilder = new NpgsqlConnectionStringBuilder(options.ConnectionString);
            DbName = connectionBuilder.Database;
            _connectionString = connectionBuilder.ConnectionString;
            
            var tempConnectionBuilder =
                new NpgsqlConnectionStringBuilder(options.ConnectionString)
                {
                    Database = PostgreDefaultDatabase
                };
            _connectionStringWithoutInitialCatalog = tempConnectionBuilder.ConnectionString;

            _options = options;
        }

        /// <inheritdoc />
        public async Task OpenConnectionAsync()
        {
            if (_connection != null && _connection.State != ConnectionState.Closed && _connection.State != ConnectionState.Broken)
                throw new InvalidOperationException("Connection have already opened");
            
            try
            {
                _connection = new NpgsqlConnection(_connectionString);
                await _connection.OpenAsync();
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.CreatingDbError, $"Can not create database {DbName}", e);
            }
        }


        /// <inheritdoc />
        public async Task<DbState> GetDbStateSafeAsync(DbVersion desireDbVersion)
        {
            try
            {
                var result = await InternalExecuteScalarScriptAsync(_connection, String.Format(CheckDbExistQueryFormat, DbName))
                    .ConfigureAwait(false);
                if (result == null || (Int32) result != 1)
                {
                    return DbState.NotCreated;
                }

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
            AssertConnection();

            var createDbQueryFormat = "CREATE DATABASE \"{0}\" "
                                      + @"WITH 
                           ENCODING = '{1}'
                           LC_COLLATE = '{2}'
                           LC_CTYPE = '{3}'
                           CONNECTION LIMIT = {4}; ";

            var createDbQueryString = String.Format(createDbQueryFormat,
                DbName,
                _options.DatabaseEncoding,
                _options.LC_COLLATE,
                _options.LC_CTYPE,
                _options.ConnectionLimit);
            try
            {
                var result =
                    await InternalExecuteScalarScriptAsync(_connection, String.Format(CheckDbExistQueryFormat, DbName));
                if (result == null || (Int32) result != 1)
                {
                    await InternalExecuteScriptAsync(createDbQueryString);
                }
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.CreatingDbError, $"Can not create database {DbName}", e);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CheckIfDatabaseExistsAsync(string databaseName)
        {
            try
            {
                var result =
                    await InternalExecuteScalarScriptAsync(_connection, String.Format(CheckDbExistQueryFormat, DbName));
                return result != null && (Int32) result == 1;
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.Unknown, $"Can not check existence of {DbName} database", e);
            }
        }

        private void AssertConnection()
        {
            if (_connection == null || _connection.State == ConnectionState.Closed || _connection.State == ConnectionState.Broken)
                throw new InvalidOperationException($"Connection is not opened. Use {nameof(OpenConnectionAsync)}");
        }
        
        private async Task ExecuteScriptAsync(string connectionString, string script)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();
                await InternalExecuteScriptAsync(script);
            }
        }

        private async Task InternalExecuteScriptAsync(string script)
        {
            AssertConnection();
            
            var command = _connection.CreateCommand();
            command.CommandText = script;
            await command
                .ExecuteNonQueryAsync();
        }

        /// <inheritdoc />
        public async Task CreateHistoryTableIfNotExistsAsync()
        {
            AssertConnection();
            
            var script = $"CREATE TABLE IF NOT EXISTS public.\"{_options.MigrationHistoryTableName}\" "
                         + @"( 
                         version text 
                        ) 
                        WITH ( 
                          OIDS=FALSE 
                        ); ";
            try
            {
                await ExecuteScriptAsync(script);
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.CreatingHistoryTable, $"Can not create database {DbName}",
                    e);
            }
        }

        /// <inheritdoc />
        public async Task<bool> CheckIfTableExistsAsync(string tableName)
        {
            if (String.IsNullOrWhiteSpace(tableName)) throw new ArgumentNullException(nameof(tableName));
            
            try
            {
                var checkTableExistenceQuery = @"SELECT EXISTS (
                                                       SELECT 1
                                                       FROM   information_schema.tables "
                                               + $" WHERE  table_schema = '{GetSchemeNameFromConnectionString()}'"
                                               + $" AND    table_name = '{tableName}');";
                var result = await ExecuteScalarScriptAsync(checkTableExistenceQuery);
                return result != null && (Int32) result == 1;
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.Unknown, $"Can not check existence of {DbName} database",
                    e);
            }
        }

        private string GetSchemeNameFromConnectionString()
        {
            const string schemeParseRegexPattern = "initial schema ?= ?(.+)";
            var regex = new Regex(schemeParseRegexPattern);
            var matches = regex.Match(_connectionString);

            return matches.Success && matches.Groups.Count > 1 && matches.Groups[1].Success
                ? matches.Groups[1].Value
                : "public";
        }

        /// <inheritdoc />
        public async Task<DbVersion?> GetDbVersionSafeAsync()
        {
            try
            {
                var version = await InternalGetDbVersionAsync();

                return version;
            }
            catch (Exception)
            {
                return default(DbVersion?);
            }
        }

        private async Task<DbVersion?> InternalGetDbVersionAsync()
        {
            var query = $"SELECT * FROM public.\"{_options.MigrationHistoryTableName}\" LIMIT 1;";

            var command = _connection.CreateCommand();
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
            AssertConnection();
            
            var script = $"DELETE FROM public.\"{_options.MigrationHistoryTableName}\"; "
                         + $"INSERT INTO public.\"{_options.MigrationHistoryTableName}\"("
                         + "version) "
                         + $" VALUES ('{version.ToString()}'); ";

            try
            {
                await ExecuteScriptAsync(script);
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.MigratingError, "Can not update DB version", e);
            }
        }

        /// <inheritdoc />
        public async Task ExecuteScriptAsync(string script)
        {
            AssertConnection();
            
            try
            {
                await ExecuteScriptAsync(script);
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.MigratingError, "Can not execute script", e);
            }
        }

        /// <inheritdoc />
        public async Task<object> ExecuteScalarScriptAsync(string script)
        {
            AssertConnection();
            
            try
            {
                var result = await InternalExecuteScalarScriptAsync(_connection, script);
                return result;
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.MigratingError, "Can not execute script", e);
            }
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
            AssertConnection();
            
            try
            {
                await ExecuteScriptAsync(_connectionStringWithoutInitialCatalog, script);
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.MigratingError, "Can not execute script", e);
            }
        }

        /// <inheritdoc />
        public async Task<object> ExecuteScalarScriptWithoutInitialCatalogAsync(string script)
        {
            AssertConnection();
            
            try
            {
                using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection.OpenAsync();
                    var result = await InternalExecuteScalarScriptAsync(connection, script);
                    
                    return result;
                }
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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.MigratingError, "Can not execute script", e);
            }
        }

        /// <inheritdoc />
        public Task CloseConnectionAsync()
        { 
            try
            {
                if (_connection.State == ConnectionState.Closed) return Task.CompletedTask;
                
                _connection.Close();
                
                return Task.CompletedTask;

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
                      || e.SqlState == "OP000"
                      || e.SqlState.StartsWith("42"))
            {
                throw new MigrationException(MigrationError.AuthorizationError,
                    $"Invalid authorization specification for {DbName}", e);
            }
            catch (NpgsqlException e)
            {
                throw new MigrationException(MigrationError.ConnectionError, $"Can not connect to DB {DbName}", e);
            }
            catch (Exception e)
            {
                throw new MigrationException(MigrationError.MigratingError, "Can not execute script", e);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}