using System;
using Curiosity.Migrations.PostgreSQL;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests
{
    public class PostgreSqlProviderDbTestFixture : IDisposable
    {
        public  IDbProvider DbProvider { get; }
        public PostgreDbProviderOptions Options { get; }
        public string DbName { get; }
        
        public PostgreSqlProviderDbTestFixture()
        {
            var random = new Random();
            DbName = $"temp_{random.Next(100)}";
            Options = new PostgreDbProviderOptions(
                String.Format(ConfigProvider.GetConfig().ConnectionStringMask, DbName), 
                lcCollate: "C",
                lcCtype: "C",
                template: "template0",
                databaseEncoding: "SQL_ASCII");
            DbProvider = new PostgreDbProvider(Options);
        }
        
        public void Dispose()
        {
            try
            {
                try
                {

                    DbProvider.OpenConnectionAsync().GetAwaiter().GetResult();
                    DbProvider.ExecuteScriptAsync($"DROP TABLE IF EXISTS {DbName}").GetAwaiter().GetResult();
                    DbProvider.CloseConnectionAsync().GetAwaiter().GetResult();
                }
                catch(Exception){}
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}