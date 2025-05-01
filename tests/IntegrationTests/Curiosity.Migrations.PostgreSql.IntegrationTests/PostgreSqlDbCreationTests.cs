using System;
using System.Threading.Tasks;
using Curiosity.Migrations.PostgreSQL;
using Curiosity.Migrations.PostgreSql.IntegrationTests.Fixtures;
using Xunit;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests;

public class PostgreSqlDbCreationTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _containerFixture;
    
    public PostgreSqlDbCreationTests(PostgresContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    [Fact]
    public async Task CreateDb_WithoutParams_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new PostgresMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName));
            migrationConnection = new PostgresMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to postgres to drop the test database
                    var postgresOptions = new PostgresMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("postgres"));
                    using var postgresConnection = new PostgresMigrationConnection(postgresOptions);
                    await postgresConnection.OpenConnectionAsync();
                    
                    // Disconnect any other connections to the database
                    var disconnectQuery = $@"
                        SELECT pg_terminate_backend(pg_stat_activity.pid)
                        FROM pg_stat_activity
                        WHERE pg_stat_activity.datname = '{dbName}'
                          AND pid <> pg_backend_pid()";
                    
                    await postgresConnection.ExecuteNonQuerySqlAsync(disconnectQuery, null);
                    
                    // Drop the database
                    await postgresConnection.ExecuteNonQuerySqlAsync(
                        $"DROP DATABASE IF EXISTS \"{dbName}\"",
                        null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
        
    [Fact]
    public async Task CreateDb_WithTemplate_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new PostgresMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName),
                template: "template0");
            migrationConnection = new PostgresMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to postgres to drop the test database
                    var postgresOptions = new PostgresMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("postgres"));
                    using var postgresConnection = new PostgresMigrationConnection(postgresOptions);
                    await postgresConnection.OpenConnectionAsync();
                    
                    // Disconnect any other connections to the database
                    var disconnectQuery = $@"
                        SELECT pg_terminate_backend(pg_stat_activity.pid)
                        FROM pg_stat_activity
                        WHERE pg_stat_activity.datname = '{dbName}'
                          AND pid <> pg_backend_pid()";
                    
                    await postgresConnection.ExecuteNonQuerySqlAsync(disconnectQuery, null);
                    
                    // Drop the database
                    await postgresConnection.ExecuteNonQuerySqlAsync(
                        $"DROP DATABASE IF EXISTS \"{dbName}\"",
                        null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
        
    [Fact]
    public async Task CreateDb_WithEncoding_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new PostgresMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName),
                template: "template0",
                databaseEncoding: "SQL_ASCII");
            migrationConnection = new PostgresMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to postgres to drop the test database
                    var postgresOptions = new PostgresMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("postgres"));
                    using var postgresConnection = new PostgresMigrationConnection(postgresOptions);
                    await postgresConnection.OpenConnectionAsync();
                    
                    // Disconnect any other connections to the database
                    var disconnectQuery = $@"
                        SELECT pg_terminate_backend(pg_stat_activity.pid)
                        FROM pg_stat_activity
                        WHERE pg_stat_activity.datname = '{dbName}'
                          AND pid <> pg_backend_pid()";
                    
                    await postgresConnection.ExecuteNonQuerySqlAsync(disconnectQuery, null);
                    
                    // Drop the database
                    await postgresConnection.ExecuteNonQuerySqlAsync(
                        $"DROP DATABASE IF EXISTS \"{dbName}\"",
                        null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
        
    [Fact]
    public async Task CreateDb_WithAllParams_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new PostgresMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName),
                template: "template0",
                connectionLimit: 10,
                lcCollate: "C",
                lcCtype: "C", 
                databaseEncoding: "SQL_ASCII");
            migrationConnection = new PostgresMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to postgres to drop the test database
                    var postgresOptions = new PostgresMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("postgres"));
                    using var postgresConnection = new PostgresMigrationConnection(postgresOptions);
                    await postgresConnection.OpenConnectionAsync();
                    
                    // Disconnect any other connections to the database
                    var disconnectQuery = $@"
                        SELECT pg_terminate_backend(pg_stat_activity.pid)
                        FROM pg_stat_activity
                        WHERE pg_stat_activity.datname = '{dbName}'
                          AND pid <> pg_backend_pid()";
                    
                    await postgresConnection.ExecuteNonQuerySqlAsync(disconnectQuery, null);
                    
                    // Drop the database
                    await postgresConnection.ExecuteNonQuerySqlAsync(
                        $"DROP DATABASE IF EXISTS \"{dbName}\"",
                        null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
