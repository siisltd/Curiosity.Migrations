using System;
using System.Collections.Generic;
using System.Linq;
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
            var provider = _fixture.MigrationConnection;
            var isDbExist = await provider.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.False(isDbExist);

            await provider.CreateDatabaseIfNotExistsAsync();
            
            isDbExist = await provider.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.True(isDbExist);

            await provider.OpenConnectionAsync();
            
            var isTableExist = await provider.CheckIfTableExistsAsync(_fixture.MigrationConnection.MigrationHistoryTableName);
            Assert.False(isTableExist);

            await provider.CreateMigrationHistoryTableIfNotExistsAsync();
            
            isTableExist = await provider.CheckIfTableExistsAsync(_fixture.MigrationConnection.MigrationHistoryTableName);
            Assert.True(isTableExist);

            var desiredDbVersion = new DbVersion(1, 0);

            await provider.SaveAppliedMigrationVersionAsync($"Version {desiredDbVersion.Major}.{desiredDbVersion.Minor}", desiredDbVersion);
            var actualAppliedMigrations = await provider.GetAppliedMigrationVersionsAsync();
            
            Assert.NotNull(actualAppliedMigrations);
            Assert.Equal(1, actualAppliedMigrations.Count);
            Assert.Equal(desiredDbVersion, actualAppliedMigrations.First());

            var queryParams = new Dictionary<string, object>
            {
                { "@databaseName", provider.DatabaseName }
            };
            var result = await provider.ExecuteScalarSqlWithoutInitialCatalogAsync(
                    "SELECT 1 AS result FROM pg_database WHERE datname=@databaseName",
                    queryParams);
            Assert.True(result is int i && i == 1 || result is bool b && !b);
            queryParams.Clear();

            await provider.ExecuteNonQuerySqlAsync("CREATE TABLE dummy (id bigint, val varchar);", null);
            for (var idx = 0; idx < 10; idx++)
            {
                queryParams["id"] = idx;
                queryParams["val"] = $"{idx}_text";
                var inserted = await provider.ExecuteNonQuerySqlAsync("INSERT INTO dummy(id, val) VALUES (@id, @val)", queryParams);
                Assert.True(inserted == 1);
            }
            queryParams.Clear();

            queryParams["id"] = 6;
            var updated = await provider.ExecuteNonQuerySqlAsync("UPDATE dummy SET val = NULL WHERE id > @id", queryParams);
            Assert.True(updated == 3);

            await provider.CloseConnectionAsync();
        }
    }
}
