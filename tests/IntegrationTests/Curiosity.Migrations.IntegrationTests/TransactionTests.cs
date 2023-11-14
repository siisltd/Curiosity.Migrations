using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Curiosity.Migrations.PostgreSQL;
using Curiosity.Migrations.TransactionTests.TransactionCodeMigrations;
using Xunit;

namespace Curiosity.Migrations.TransactionTests;

public class TransactionTests
{
    [Fact]
    public async Task Migrate_AllScriptOk_NoRollback()
    {
        var config = ConfigProvider.GetConfig();
        var connectionString = String.Format(config.ConnectionStringMask, "test_ok");


        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly<ITransactionMigration>(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "TransactionScriptMigrations"));
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
        var config = ConfigProvider.GetConfig();
        var connectionString = String.Format(config.ConnectionStringMask, "test_without_transactions");


        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "ScriptMigrations"));
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
        var config = ConfigProvider.GetConfig();
        var connectionString = String.Format(config.ConnectionStringMask, "test_rollback");


        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "ScriptMigrations"));
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
