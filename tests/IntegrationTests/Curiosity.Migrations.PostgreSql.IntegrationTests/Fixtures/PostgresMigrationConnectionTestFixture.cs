using System;
using System.Threading.Tasks;
using Curiosity.Migrations.PostgreSQL;
using Xunit;

namespace Curiosity.Migrations.PostgreSql.IntegrationTests.Fixtures;

public class PostgresMigrationConnectionTestFixture : IAsyncLifetime
{
    private readonly PostgresContainerFixture _containerFixture;
    public IMigrationConnection MigrationConnection { get; private set; }
    internal PostgresMigrationConnectionOptions Options { get; private set; }
    public string DbName { get; }

    public PostgresMigrationConnectionTestFixture()
    {
        var random = new Random();
        DbName = $"temp_{random.Next(100)}";
        _containerFixture = new PostgresContainerFixture();
    }

    public async Task InitializeAsync()
    {
        // Start the container
        await _containerFixture.InitializeAsync();
        
        // Create connection options with the dynamic connection string from the container
        Options = new PostgresMigrationConnectionOptions(
            _containerFixture.GetConnectionString(DbName),
            lcCollate: "C",
            lcCtype: "C",
            template: "template0",
            databaseEncoding: "SQL_ASCII");
            
        MigrationConnection = new PostgresMigrationConnection(Options);
    }

    public async Task DisposeAsync()
    {
        await _containerFixture.DisposeAsync();
    }
}
