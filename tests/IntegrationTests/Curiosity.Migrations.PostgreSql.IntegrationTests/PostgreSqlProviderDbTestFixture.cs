using System;
using Curiosity.Migrations.PostgreSQL;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests
{
    public class PostgreSqlProviderDbTestFixture : IDisposable
    {
        public  IMigrationConnection MigrationConnection { get; }
        internal PostgresMigrationConnectionOptions Options { get; }
        public string DbName { get; }
        
        public PostgreSqlProviderDbTestFixture()
        {
            var random = new Random();
            DbName = $"temp_{random.Next(100)}";
            Options = new PostgresMigrationConnectionOptions(
                String.Format(ConfigProvider.GetConfig().ConnectionStringMask, DbName), 
                lcCollate: "C",
                lcCtype: "C",
                template: "template0",
                databaseEncoding: "SQL_ASCII");
            MigrationConnection = new PostgresMigrationConnection(Options);
        }
        
        public void Dispose()
        {
            try
            {
                try
                {

                    MigrationConnection.OpenConnectionAsync().GetAwaiter().GetResult();
                    MigrationConnection.ExecuteNonQuerySqlAsync(
                        $"DROP TABLE IF EXISTS {DbName}",
                        null)
                        .GetAwaiter()
                        .GetResult();
                    MigrationConnection.CloseConnectionAsync().GetAwaiter().GetResult();
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
