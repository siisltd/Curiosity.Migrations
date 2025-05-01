using System;
using System.Threading.Tasks;
using Xunit;

namespace Curiosity.Migrations.SqlServer.IntegrationTests.Fixtures;

public class SqlServerMigrationConnectionTestFixture : IAsyncLifetime
{
    private readonly SqlServerContainerFixture _containerFixture;
    public IMigrationConnection MigrationConnection { get; private set; }
    internal SqlServerMigrationConnectionOptions Options { get; private set; }
    public string DbName { get; }

    public SqlServerMigrationConnectionTestFixture()
    {
        var random = new Random();
        DbName = $"temp_{random.Next(100)}";
        _containerFixture = new SqlServerContainerFixture();
    }

    public async Task InitializeAsync()
    {
        // Start the container
        await _containerFixture.InitializeAsync();
        
        // Create connection options with the dynamic connection string from the container
        Options = new SqlServerMigrationConnectionOptions(
            _containerFixture.GetConnectionString(DbName),
            collation: "SQL_Latin1_General_CP1_CI_AS",
            initialSize: 10,
            fileGrowth: 5,
            maxSize: 100,
            allowSnapshotIsolation: true,
            readCommittedSnapshot: true);
            
        MigrationConnection = new SqlServerMigrationConnection(Options);
    }

    public Task DisposeAsync()
    {
        try
        {
            // Connect to master and drop the test database
            var masterOptions = new SqlServerMigrationConnectionOptions(
                _containerFixture.GetConnectionString("master"));
            
            using var masterConnection = new SqlServerMigrationConnection(masterOptions);
            masterConnection.OpenConnectionAsync().GetAwaiter().GetResult();
            
            var dropQuery = $@"
                IF EXISTS (SELECT 1 FROM sys.databases WHERE name = '{DbName}') 
                BEGIN 
                    ALTER DATABASE [{DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{DbName}]; 
                END";
            
            masterConnection.ExecuteNonQuerySqlAsync(dropQuery, null).GetAwaiter().GetResult();
            
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error during test cleanup: {e.Message}");
            return Task.CompletedTask;
        }
    }
} 