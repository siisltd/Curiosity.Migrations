using Curiosity.Migrations.SqlServer.IntegrationTests.Fixtures;
using Xunit;

namespace Curiosity.Migrations.SqlServer.IntegrationTests;

public class SqlServerDbCreationTests : IClassFixture<SqlServerContainerFixture>, IAsyncLifetime
{
    private readonly SqlServerContainerFixture _containerFixture;
    
    public SqlServerDbCreationTests(SqlServerContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }
    
    public Task InitializeAsync()
    {
        return _containerFixture.InitializeAsync();
    }
    
    public Task DisposeAsync() 
    {
        return Task.CompletedTask; // Container will be disposed by the singleton fixture
    }

    [Fact]
    public async Task CreateDb_WithoutParams_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new SqlServerMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName));
            migrationConnection = new SqlServerMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to master to drop the test database
                    var masterOptions = new SqlServerMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("master"));
                    using var masterConnection = new SqlServerMigrationConnection(masterOptions);
                    await masterConnection.OpenConnectionAsync();
                    await masterConnection.ExecuteNonQuerySqlAsync($"DROP DATABASE IF EXISTS [{dbName}]", null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    [Fact]
    public async Task CreateDb_WithCollation_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new SqlServerMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName),
                collation: "SQL_Latin1_General_CP1_CI_AS");
            migrationConnection = new SqlServerMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to master to drop the test database
                    var masterOptions = new SqlServerMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("master"));
                    using var masterConnection = new SqlServerMigrationConnection(masterOptions);
                    await masterConnection.OpenConnectionAsync();
                    await masterConnection.ExecuteNonQuerySqlAsync($"DROP DATABASE IF EXISTS [{dbName}]", null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    [Fact]
    public async Task CreateDb_WithIsolationLevels_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new SqlServerMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName),
                allowSnapshotIsolation: true,
                readCommittedSnapshot: true);
            migrationConnection = new SqlServerMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to master to drop the test database
                    var masterOptions = new SqlServerMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("master"));
                    using var masterConnection = new SqlServerMigrationConnection(masterOptions);
                    await masterConnection.OpenConnectionAsync();
                    await masterConnection.ExecuteNonQuerySqlAsync($"DROP DATABASE IF EXISTS [{dbName}]", null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    [Fact]
    public async Task CreateDb_WithFileOptions_Ok()
    {
        IMigrationConnection? migrationConnection = null;
        var random = new Random();
        var dbName = $"temp_{random.Next(100)}";
        try
        {
            var options = new SqlServerMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName),
                initialSize: 10,
                maxSize: 100,
                fileGrowth: 5);
            migrationConnection = new SqlServerMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to master to drop the test database
                    var masterOptions = new SqlServerMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("master"));
                    using var masterConnection = new SqlServerMigrationConnection(masterOptions);
                    await masterConnection.OpenConnectionAsync();
                    await masterConnection.ExecuteNonQuerySqlAsync($"DROP DATABASE IF EXISTS [{dbName}]", null);
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
            var options = new SqlServerMigrationConnectionOptions(
                _containerFixture.GetConnectionString(dbName),
                collation: "SQL_Latin1_General_CP1_CI_AS",
                maxConnections: 10,
                initialSize: 10,
                maxSize: 100,
                fileGrowth: 5,
                allowSnapshotIsolation: true,
                readCommittedSnapshot: true);
            migrationConnection = new SqlServerMigrationConnection(options);
            await migrationConnection.CreateDatabaseIfNotExistsAsync();
        }
        finally
        {
            if (migrationConnection != null)
            {
                try
                {
                    // Connect to master to drop the test database
                    var masterOptions = new SqlServerMigrationConnectionOptions(
                        _containerFixture.GetConnectionString("master"));
                    using var masterConnection = new SqlServerMigrationConnection(masterOptions);
                    await masterConnection.OpenConnectionAsync();
                    await masterConnection.ExecuteNonQuerySqlAsync($"DROP DATABASE IF EXISTS [{dbName}]", null);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
} 