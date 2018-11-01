using System;
using System.Threading.Tasks;
using Marvin.Migrations.Exceptions;
using Marvin.Migrations.Info;
using Npgsql;

namespace Marvin.Migrations.PostgreSQL
{
    /// <summary>
    /// 
    /// </summary>
    //todo reduce connections usage
    public class PostgreDbProvider : IDbProvider
    {
        private const string CheckDbExistQueryFormat = "SELECT 1 AS result FROM pg_database WHERE datname='{0}'";

        private const string MigrationTableName = "MigrationHistory";

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

        public PostgreDbProvider(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;

            var connectionBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            DbName = connectionBuilder.Database;
            _connectionString = connectionBuilder.ConnectionString;

            var tempConnectionBuilder =
                new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Database = PostgreDefaultDatabase
                };
            _connectionStringWithoutInitialCatalog = tempConnectionBuilder.ConnectionString;
        }


        public async Task<DbState> GetDbStateSafeAsync(DbVersion desireDbVersion)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection
                        .OpenAsync()
                        .ConfigureAwait(false);

                    var result = await InternalExecuteScalarScriptAsync(
                            connection,
                            String.Format(CheckDbExistQueryFormat, DbName))
                        .ConfigureAwait(false);
                    if (result == null || (Int32) result != 1)
                    {
                        return DbState.NotCreated;
                    }
                }

                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection
                        .OpenAsync()
                        .ConfigureAwait(false);
                    var dbVersion = await InternalGetDbVersionAsync(connection)
                        .ConfigureAwait(false);
                    if (dbVersion == null) return DbState.Outdated;
                    if (dbVersion.Value == desireDbVersion) return DbState.Ok;
                    return dbVersion.Value < desireDbVersion
                        ? DbState.Outdated
                        : DbState.Newer;
                }
            }
            catch (Exception)
            {
                return DbState.Unknown;
            }
        }

        public async Task CreateDatabaseIfNotExistsAsync()
        {
            var createDbQuery = $"CREATE DATABASE \"{DbName}\" "
                                + @"WITH 
                           ENCODING = 'UTF8'
                           LC_COLLATE = 'Russian_Russia.1251'
                           LC_CTYPE = 'Russian_Russia.1251'
                           CONNECTION LIMIT = -1; ";

            try
            {
                using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection
                        .OpenAsync()
                        .ConfigureAwait(false);

                    var result = await InternalExecuteScalarScriptAsync(
                            connection,
                            String.Format(CheckDbExistQueryFormat, DbName))
                        .ConfigureAwait(false);
                    if (result == null || (Int32) result != 1)
                    {
                        await InternalExecuteScriptAsync(connection, createDbQuery)
                            .ConfigureAwait(false);
                    }
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
                throw new MigrationException(MigrationError.CreatingDBError, $"Can not create database {DbName}", e);
            }
        }



        private async Task ExecuteScriptAsync(string connectionString, string script)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection
                    .OpenAsync()
                    .ConfigureAwait(false);
                await InternalExecuteScriptAsync(connection, script)
                    .ConfigureAwait(false);
            }
        }

        private async Task InternalExecuteScriptAsync(NpgsqlConnection opennedConnection, string script)
        {
            var command = opennedConnection.CreateCommand();
            command.CommandText = script;
            await command
                .ExecuteNonQueryAsync()
                .ConfigureAwait(false);
        }

        public async Task CreateHistoryTableIfNotExistsAsync()
        {
            var script = $"CREATE TABLE IF NOT EXISTS public.\"{MigrationTableName}\" "
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

        public async Task<DbVersion?> GetDbVersionSafeAsync()
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection
                        .OpenAsync()
                        .ConfigureAwait(false);
                    var version = await InternalGetDbVersionAsync(connection)
                        .ConfigureAwait(false);

                    return version;
                }
            }
            catch (Exception)
            {
                return default(DbVersion?);
            }
        }

        private async Task<DbVersion?> InternalGetDbVersionAsync(NpgsqlConnection opennedConnection)
        {
            var query = $"SELECT * FROM public.\"{MigrationTableName}\" LIMIT 1;";

            var command = opennedConnection.CreateCommand();
            command.CommandText = query;

            using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                if (!reader.HasRows) return default(DbVersion?);

                await reader.ReadAsync().ConfigureAwait(false);
                var stringVersion = reader.GetString(0);

                if (!DbVersion.TryParse(stringVersion, out var version))
                {
                    throw new InvalidOperationException("Cannot get database version.");
                }

                return version;
            }
        }

        public async Task UpdateCurrentDbVersionAsync(DbVersion version)
        {
            var script = $"DELETE FROM public.\"{MigrationTableName}\"; "
                         + $"INSERT INTO public.\"{MigrationTableName}\"("
                         + "version) "
                         + $" VALUES ('{version.ToString()}'); ";

            try
            {
                await ExecuteScriptAsync(_connectionString, script);
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

        public async Task ExecuteScriptAsync(string script)
        {
            try
            {

                await ExecuteScriptAsync(_connectionString, script);
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

        public async Task<object> ExecuteScalarScriptAsync(string script)
        {
            try
            {

                using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection
                        .OpenAsync()
                        .ConfigureAwait(false);

                    var result = await InternalExecuteScalarScriptAsync(connection, script)
                        .ConfigureAwait(false);
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

        private Task<object> InternalExecuteScalarScriptAsync(
            NpgsqlConnection openedConnection,
            string query)
        {
            var command = openedConnection.CreateCommand();
            command.CommandText = query;
            return command
                .ExecuteScalarAsync();
        }

        public async Task ExecuteScriptWithoutInitialCatalogAsync(string script)
        {
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

        public async Task<object> ExecuteScalarScriptWithoutInitialCatalogAsync(string script)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionStringWithoutInitialCatalog))
                {
                    await connection
                        .OpenAsync()
                        .ConfigureAwait(false);

                    var result = await InternalExecuteScalarScriptAsync(connection, script)
                        .ConfigureAwait(false);
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
    }
}