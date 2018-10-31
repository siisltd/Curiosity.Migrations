using System;
using System.Threading.Tasks;
using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;
using Npgsql;

namespace Marvin.Migrations.PostgreSQL
{
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
        private const string PostgressDefaultDatabase = "postgres";

        public string DbName { get; }

        private readonly string _connectionString;
        private readonly string _connectionStringWitoutInitialCatalog;

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
                    Database = PostgressDefaultDatabase
                };
            _connectionStringWitoutInitialCatalog = tempConnectionBuilder.ConnectionString;
        }


        public async Task<DbState> GetDbStateAsync(DbVersion desireDbVersion)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionStringWitoutInitialCatalog))
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

            using (var connection = new NpgsqlConnection(_connectionStringWitoutInitialCatalog))
            {
                await connection
                    .OpenAsync()
                    .ConfigureAwait(false);

                var result = await InternalExecuteScalarScriptAsync(
                        connection,
                        String.Format(CheckDbExistQueryFormat, DbName))
                    .ConfigureAwait(false);
                if (result == null || (Int32)result != 1)
                {
                    await InternalExecuteScriptAsync(connection, createDbQuery)
                        .ConfigureAwait(false);
                }
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
        
        public Task CreateHistoryTableIfNotExistsAsync()
        {
            var script = $"CREATE TABLE IF NOT EXISTS public.\"{MigrationTableName}\" "
                        + @"( 
                         version text 
                        ) 
                        WITH ( 
                          OIDS=FALSE 
                        ); ";
            return ExecuteScriptAsync(script);
        }

        public async Task<DbVersion?> GetDbVersionAsync()
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

        public Task UpdateCurrentDbVersionAsync(DbVersion version)
        {
            var script = $"DELETE FROM public.\"{MigrationTableName}\"; "
                       + $"INSERT INTO public.\"{MigrationTableName}\"("
                       + "version) "
                       + $" VALUES ('{version.ToString()}'); ";

            return ExecuteScriptAsync(_connectionString, script);
        }

        public Task ExecuteScriptAsync(string script)
        {
            return ExecuteScriptAsync(_connectionString, script);
        }

        public async Task<object> ExecuteScalarScriptAsync(string script)
        {
            using (var connection = new NpgsqlConnection(_connectionStringWitoutInitialCatalog))
            {
                await connection
                    .OpenAsync()
                    .ConfigureAwait(false);

                var result = await InternalExecuteScalarScriptAsync(connection, script)
                    .ConfigureAwait(false);
                return result;
            }
        }

        private Task<object> InternalExecuteScalarScriptAsync(
            NpgsqlConnection opennedConnection, 
            string query)
        {
            var command = opennedConnection.CreateCommand();
            command.CommandText = query;
            return command
                .ExecuteScalarAsync();
        }

        public Task ExecuteScriptWithoutInitialCatalogAsync(string script)
        {
            return ExecuteScriptAsync(_connectionStringWitoutInitialCatalog, script);
        }

        public async Task<object> ExecuteScalarScriptWithoutInitialCatalogAsync(string script)
        {
            using (var connection = new NpgsqlConnection(_connectionStringWitoutInitialCatalog))
            {
                await connection
                    .OpenAsync()
                    .ConfigureAwait(false);

                var result = await InternalExecuteScalarScriptAsync(connection, script)
                    .ConfigureAwait(false);
                return result;
            }
        }
    }
}