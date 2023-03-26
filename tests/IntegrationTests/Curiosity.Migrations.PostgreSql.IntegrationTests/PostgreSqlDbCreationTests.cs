using System;
using System.Threading.Tasks;
using Curiosity.Migrations.PostgreSQL;
using Xunit;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests;

public class PostgreSqlDbCreationTests
{
    [Fact]
    public async Task CreateDb_WithoutParams_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new PostgresMigrationConnectionOptions(
                String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName));
            migrationConnection = new PostgresMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {

                    await migrationConnection.OpenConnectionAsync();
                    await migrationConnection.ExecuteNonQuerySqlAsync($"DROP TABLE IF EXISTS {dbName}", null);
                    await migrationConnection.CloseConnectionAsync();
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
                String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName),
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

                    await migrationConnection.OpenConnectionAsync();
                    await migrationConnection.ExecuteNonQuerySqlAsync($"DROP TABLE IF EXISTS {dbName}", null);
                    await migrationConnection.CloseConnectionAsync();
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
                String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName),
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

                    await migrationConnection.OpenConnectionAsync();
                    await migrationConnection.ExecuteNonQuerySqlAsync($"DROP TABLE IF EXISTS {dbName}", null);
                    await migrationConnection.CloseConnectionAsync();
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
                String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName),
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

                    await migrationConnection.OpenConnectionAsync();
                    await migrationConnection.ExecuteNonQuerySqlAsync($"DROP TABLE IF EXISTS {dbName}", null);
                    await migrationConnection.CloseConnectionAsync();
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
