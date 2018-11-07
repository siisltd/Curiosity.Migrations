using System;
using Marvin.Migrations.PostgreSQL;

namespace Marvin.Migrations.PostgreSql.IntegrationTests
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
            Options = new PostgreDbProviderOptions($"connection_string", lcCollate: "C", lcCtype: "C", databaseEncoding: "SQL_ASCII");
            DbProvider = new PostgreDbProvider(Options);
        }
        
        public void Dispose()
        {
            try
            {
                try
                {

                    DbProvider.OpenConnectionAsync().GetAwaiter().GetResult();
                }
                catch(Exception e){}
                DbProvider.ExecuteScriptAsync($"DROP TABLE IF EXISTS {DbName}").GetAwaiter().GetResult();
                DbProvider.CloseConnectionAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}