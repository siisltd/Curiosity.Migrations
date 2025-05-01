using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Curiosity.Migrations.IntegrationTests.Fixtures;
using Curiosity.Migrations.IntegrationTests.TransactionsTests.TransactionCodeMigrations;
using Curiosity.Migrations.PostgreSQL;
using Xunit;

namespace Curiosity.Migrations.IntegrationTests.TransactionsTests;

public class TransactionTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _containerFixture;
    
    public TransactionTests(PostgreSqlContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    [Fact]
    public async Task Migrate_AllScriptOk_NoRollback()
    {
        var connectionString = _containerFixture.GetConnectionString("test_ok");

        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TransactionsTests/TransactionScriptMigrations"));
        builder.ConfigureForPostgreSql(connectionString);

        builder.UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.SetUpTargetVersion(new MigrationVersion(3));

        var migrator = builder.Build();

        var result = await migrator.UpgradeDatabaseAsync();

        var migrationProvider = new PostgresMigrationConnection(new PostgresMigrationConnectionOptions(connectionString));
        await migrationProvider.OpenConnectionAsync();
        var actualAppliedMigrations = await migrationProvider.GetAppliedMigrationVersionsAsync();
        await migrationProvider.CloseConnectionAsync();

        var expectedAppliedMigrations = new HashSet<MigrationVersion>
        {
            new(1),
            new(2),
            new(3)
        };
        Assert.True(result.IsSuccessfully);
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }

    [Fact]
    public async Task Migrate_AllScriptOk_SwitchedOffTransaction()
    {
        var connectionString = _containerFixture.GetConnectionString("test_without_transactions");

        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TransactionsTests/TransactionScriptMigrations"));
        builder.ConfigureForPostgreSql(connectionString);

        builder.UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.SetUpTargetVersion(new MigrationVersion(5));

        var migrator = builder.Build();

        await migrator.UpgradeDatabaseAsync();

        var migrationProvider = new PostgresMigrationConnection(new PostgresMigrationConnectionOptions(connectionString));
        await migrationProvider.OpenConnectionAsync();
        var actualAppliedMigrations = await migrationProvider.GetAppliedMigrationVersionsAsync();
        await migrationProvider.CloseConnectionAsync();

        var expectedAppliedMigrations = new HashSet<MigrationVersion>
        {
            new(1),
            new(2),
            new(3),
            new(4),
            new(5)
        };
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }

    [Fact]
    public async Task Migrate_AllScriptOk_Rollback()
    {
        var connectionString = _containerFixture.GetConnectionString("test_rollback");

        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TransactionsTests/TransactionScriptMigrations"));
        builder.ConfigureForPostgreSql(connectionString);

        builder.UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.SetUpTargetVersion(new MigrationVersion(6));

        var migrator = builder.Build();

        try
        {
            await migrator.UpgradeDatabaseAsync();

            // last migration is incorrect, can not go here
            Assert.False(true);
        }
        catch
        {
            // ignored
        }

        var migrationProvider = new PostgresMigrationConnection(new PostgresMigrationConnectionOptions(connectionString));
        await migrationProvider.OpenConnectionAsync();
        var actualAppliedMigrations = await migrationProvider.GetAppliedMigrationVersionsAsync();
        await migrationProvider.CloseConnectionAsync();

        var expectedAppliedMigrations = new HashSet<MigrationVersion>
        {
            new(1),
            new(2),
            new(3),
            new(4),
            new(5)
        };

        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }
}
