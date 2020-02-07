using System;
using System.Threading.Tasks;
using Xunit;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests
{
    public class PostgreSQLProviderIntegrationTests : IClassFixture<PostgreSqlProviderDbTestFixture>
    {
        private readonly PostgreSqlProviderDbTestFixture _fixture;

        public PostgreSQLProviderIntegrationTests(PostgreSqlProviderDbTestFixture fixture)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        }
        
        [Fact]
        public async Task BigBangIntegrationTest()
        {
            var provider = _fixture.DbProvider;
            var isDbExist = await provider.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.False(isDbExist);

            await provider.CreateDatabaseIfNotExistsAsync();
            
            isDbExist = await provider.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.True(isDbExist);

            await provider.OpenConnectionAsync();
            
            var isTableExist = await provider.CheckIfTableExistsAsync(_fixture.DbProvider.MigrationHistoryTableName);
            Assert.False(isTableExist);

            await provider.CreateHistoryTableIfNotExistsAsync();
            
            isTableExist = await provider.CheckIfTableExistsAsync(_fixture.DbProvider.MigrationHistoryTableName);
            Assert.True(isTableExist);

            var desiredDbVersion = new DbVersion(1, 0);
            var state = await provider.GetDbStateSafeAsync(desiredDbVersion);
            Assert.Equal(DbState.Outdated, state);

            await provider.UpdateCurrentDbVersionAsync(desiredDbVersion);
            var currentDbVersion = await provider.GetDbVersionSafeAsync();
            
            Assert.NotNull(currentDbVersion);
            Assert.Equal(desiredDbVersion, currentDbVersion.Value);

            var result = await 
                provider.ExecuteScalarScriptWithoutInitialCatalogAsync(
                    $"SELECT 1 AS result FROM pg_database WHERE datname='{provider.DbName}'");
            Assert.True(result is int i && i == 1 || result is bool b && !b);

            await provider.ExecuteScriptAsync("CREATE TABLE dummy (id bigint, val varchar);");
            for (var idx = 0; idx < 10; idx++)
            {
                var inserted = await provider.ExecuteNonQueryScriptAsync($"INSERT INTO dummy(id, val) VALUES ({idx}, '{idx}_text')");
                Assert.True(inserted == 1);
            }
            
            var updated = await provider.ExecuteNonQueryScriptAsync("UPDATE dummy SET val = NULL WHERE id > 6");
            Assert.True(updated == 3);

            await provider.CloseConnectionAsync();
        }
    }
}