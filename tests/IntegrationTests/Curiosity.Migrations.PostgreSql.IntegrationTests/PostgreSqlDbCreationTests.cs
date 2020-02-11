using System;
using System.Threading.Tasks;
using Curiosity.Migrations.PostgreSQL;
using Xunit;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests
{
    public class PostgreSqlDbCreationTests
    {
        [Fact]
        public async Task CreateDb_WithoutParams_Ok()
        {
            IDbProvider dbProvider = null;
            var random = new Random();
            var dbName = $"temp_{random.Next(100)}";
            try
            {
                var options = new PostgreDbProviderOptions(
                    String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName));
                dbProvider = new PostgreDbProvider(options);
                await dbProvider.CreateDatabaseIfNotExistsAsync();
            }
            finally
            {
                if (dbProvider != null)
                {
                    try
                    {

                        await dbProvider.OpenConnectionAsync();
                        await dbProvider.ExecuteScriptAsync($"DROP TABLE IF EXISTS {dbName}");
                        await dbProvider.CloseConnectionAsync();
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
            IDbProvider dbProvider = null;
            var random = new Random();
            var dbName = $"temp_{random.Next(100)}";
            try
            {
                var options = new PostgreDbProviderOptions(
                    String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName),
                    template: "template0");
                dbProvider = new PostgreDbProvider(options);
                await dbProvider.CreateDatabaseIfNotExistsAsync();
            }
            finally
            {
                if (dbProvider != null)
                {
                    try
                    {

                        await dbProvider.OpenConnectionAsync();
                        await dbProvider.ExecuteScriptAsync($"DROP TABLE IF EXISTS {dbName}");
                        await dbProvider.CloseConnectionAsync();
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
            IDbProvider dbProvider = null;
            var random = new Random();
            var dbName = $"temp_{random.Next(100)}";
            try
            {
                var options = new PostgreDbProviderOptions(
                    String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName),
                    template: "template0",
                    databaseEncoding: "SQL_ASCII");
                dbProvider = new PostgreDbProvider(options);
                await dbProvider.CreateDatabaseIfNotExistsAsync();
            }
            finally
            {
                if (dbProvider != null)
                {
                    try
                    {

                        await dbProvider.OpenConnectionAsync();
                        await dbProvider.ExecuteScriptAsync($"DROP TABLE IF EXISTS {dbName}");
                        await dbProvider.CloseConnectionAsync();
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
            IDbProvider dbProvider = null;
            var random = new Random();
            var dbName = $"temp_{random.Next(100)}";
            try
            {
                var options = new PostgreDbProviderOptions(
                    String.Format(ConfigProvider.GetConfig().ConnectionStringMask, dbName),
                    template: "template0",
                    connectionLimit: 10,
                    lcCollate: "C",
                    lcCtype: "C", 
                    databaseEncoding: "SQL_ASCII");
                dbProvider = new PostgreDbProvider(options);
                await dbProvider.CreateDatabaseIfNotExistsAsync();
            }
            finally
            {
                if (dbProvider != null)
                {
                    try
                    {

                        await dbProvider.OpenConnectionAsync();
                        await dbProvider.ExecuteScriptAsync($"DROP TABLE IF EXISTS {dbName}");
                        await dbProvider.CloseConnectionAsync();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
        }
    }
}