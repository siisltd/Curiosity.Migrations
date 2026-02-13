using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;
using Xunit;

namespace Curiosity.Migrations.SqlServer.IntegrationTests.Fixtures;

/// <summary>
/// SQL Server TestContainer fixture that provides a SQL Server instance for integration tests
/// </summary>
public class SqlServerContainerFixture : IAsyncLifetime
{   
    /// <summary>
    /// The default SA password for the SQL Server container
    /// </summary>
    public const string Password = "Password123";
    
    /// <summary>
    /// The SQL Server container instance
    /// </summary>
    public MsSqlContainer Container { get; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerContainerFixture"/> class
    /// </summary>
    public SqlServerContainerFixture()
    {
        Container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2019-latest")
            .WithPassword(Password)
            .WithPortBinding(1433, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(1433))
            .Build();
    }
    
    /// <summary>
    /// Gets the connection string to connect to the SQL Server container
    /// </summary>
    public string GetConnectionString(string database = "master")
    {
        return $"Server={Container.Hostname},{Container.GetMappedPublicPort(1433)};Database={database};User Id=sa;Password={Password};TrustServerCertificate=True;";
    }
    
    /// <summary>
    /// Initializes the container
    /// </summary>
    public Task InitializeAsync()
    {
            return Container.StartAsync();
    }
    
    /// <summary>
    /// Disposes the container
    /// </summary>
    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }
} 