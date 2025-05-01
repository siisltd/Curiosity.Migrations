using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Curiosity.Migrations.PostgreSql.IntegrationTests.Fixtures;
using Xunit;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests;

/// <summary>
/// Test that check main flow of migration and test all cases in same time.
/// </summary>
public class PostgresMigrationConnectionBigBangIntegrationTests : IClassFixture<PostgresMigrationConnectionTestFixture>
{
    private readonly PostgresMigrationConnectionTestFixture _fixture;

    public PostgresMigrationConnectionBigBangIntegrationTests(PostgresMigrationConnectionTestFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }
        
    [Fact]
    public async Task BigBangIntegrationTest()
    {
        var connection = _fixture.MigrationConnection;

        try
        {
            var isDbExist = await connection.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.False(isDbExist);

            await connection.CreateDatabaseIfNotExistsAsync();

            isDbExist = await connection.CheckIfDatabaseExistsAsync(_fixture.DbName);
            Assert.True(isDbExist);

            await connection.OpenConnectionAsync();

            var isTableExist =
                await connection.CheckIfTableExistsAsync(_fixture.MigrationConnection.MigrationHistoryTableName);
            Assert.False(isTableExist);

            await connection.CreateMigrationHistoryTableIfNotExistsAsync();

            isTableExist =
                await connection.CheckIfTableExistsAsync(_fixture.MigrationConnection.MigrationHistoryTableName);
            Assert.True(isTableExist);

            var desiredDbVersion = new MigrationVersion(1);

            await connection.SaveAppliedMigrationVersionAsync(desiredDbVersion,
                $"Version {desiredDbVersion.Major}.{desiredDbVersion.Minor}");
            var actualAppliedMigrations = await connection.GetAppliedMigrationVersionsAsync();

            Assert.NotNull(actualAppliedMigrations);
            Assert.Equal(1, actualAppliedMigrations.Count);
            Assert.Equal(desiredDbVersion, actualAppliedMigrations.First());

            var queryParams = new Dictionary<string, object?>
            {
                { "@databaseName", connection.DatabaseName }
            };
            var result = await connection.ExecuteScalarSqlWithoutInitialCatalogAsync(
                "SELECT 1 AS result FROM pg_database WHERE datname=@databaseName",
                queryParams);
            Assert.True(result is int i && i == 1 || result is bool b && !b);
            queryParams.Clear();

            await connection.ExecuteNonQuerySqlAsync("CREATE TABLE dummy (id bigint, val varchar);", null);
            for (var idx = 0; idx < 10; idx++)
            {
                queryParams["id"] = idx;
                queryParams["val"] = $"{idx}_text";
                var inserted =
                    await connection.ExecuteNonQuerySqlAsync("INSERT INTO dummy(id, val) VALUES (@id, @val)",
                        queryParams);
                Assert.True(inserted == 1);
            }

            queryParams.Clear();

            queryParams["id"] = 6;
            var updated =
                await connection.ExecuteNonQuerySqlAsync("UPDATE dummy SET val = NULL WHERE id > @id", queryParams);
            Assert.True(updated == 3);

            await connection.CloseConnectionAsync();
        }
        finally
        {
            await connection.CloseConnectionAsync();
        }
    }
}
