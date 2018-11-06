using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Marvin.Migrations.PostgreSQL;
using Xunit;

namespace Marvin.Migrations.PostgreSql.IntegrationTests
{
    public class PostgreSqlProviderTestFixture : IDisposable
    {
        public  IDbProvider DbProvider { get; }
        public PostgreDbProviderOptions Options { get; }
        public string DbName { get; }
        
        public PostgreSqlProviderTestFixture()
        {
            var random = new Random();
            DbName = $"temp_{random.Next(100)}";
            Options = new PostgreDbProviderOptions($"Server=dev2.siisltd.ru;Port=5432; Database={DbName}; User Id=postgres; Password=18082034");
            DbProvider = new PostgreDbProvider(Options);
        }
        
        public void Dispose()
        {
            try
            {
                DbProvider.OpenConnectionAsync().GetAwaiter().GetResult();
                DbProvider.ExecuteScriptAsync($"DROP TABLE IS EXIST {DbName}").GetAwaiter().GetResult();
                DbProvider.CloseConnectionAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
    
    public class PostgreSQLProviderTests : IClassFixture<PostgreSqlProviderTestFixture>
    {
        private readonly PostgreSqlProviderTestFixture _fixture;

        public PostgreSQLProviderTests(PostgreSqlProviderTestFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }
        
        [Fact]
        public async Task CreateDb_DbNotExists_Created()
        {
            await _fixture.DbProvider.OpenConnectionAsync();
            
            var isDbExist = await _fixture.DbProvider.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.False(isDbExist);

            await _fixture.DbProvider.CreateDatabaseIfNotExistsAsync();
            
            
            isDbExist = await _fixture.DbProvider.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.True(isDbExist);
            
            
            await _fixture.DbProvider.CloseConnectionAsync();
        }
    }
}