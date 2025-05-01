using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;
using Xunit;

namespace Curiosity.Migrations.IntegrationTests.Fixtures;

/// <summary>
/// PostgreSQL TestContainer fixture that provides a PostgreSQL instance for integration tests
/// </summary>
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    /// <summary>
    /// The default password for the PostgreSQL container
    /// </summary>
    public const string Password = "test";
    
    /// <summary>
    /// The default username for the PostgreSQL container
    /// </summary>
    public const string Username = "test";
    
    /// <summary>
    /// The PostgreSQL container instance
    /// </summary>
    public PostgreSqlContainer Container { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlContainerFixture"/> class
    /// </summary>
    public PostgreSqlContainerFixture()
    {
        Container = new PostgreSqlBuilder()
            .WithImage("postgres:14.17-bookworm")
            .WithUsername(Username)
            .WithPassword(Password)
            .WithDatabase("postgres")
            // Use random port binding to avoid conflicts
            .WithPortBinding(5432, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
            .Build();
    }
    
    /// <summary>
    /// Gets the connection string to connect to the PostgreSQL container
    /// </summary>
    public string GetConnectionString(string database = "postgres")
    {
        return $"Server={Container.Hostname};Port={Container.GetMappedPublicPort(5432)};Database={database};User Id={Username};Password={Password}";
    }
    
    /// <summary>
    /// Initializes the container
    /// </summary>
    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }
    
    /// <summary>
    /// Disposes the container
    /// </summary>
    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
} 