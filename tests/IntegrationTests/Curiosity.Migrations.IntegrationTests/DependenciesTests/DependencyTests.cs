using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Curiosity.Migrations.IntegrationTests.DependenciesTests.DependencyCodeMigrations;
using Curiosity.Migrations.IntegrationTests.Fixtures;
using Curiosity.Migrations.PostgreSQL;
using Xunit;

namespace Curiosity.Migrations.IntegrationTests.DependenciesTests;

public class DependencyTests : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _containerFixture;
    
    public DependencyTests(PostgreSqlContainerFixture containerFixture)
    {
        _containerFixture = containerFixture;
    }

    [Fact]
    public async Task Migrate_Script_OkDependencies()
    {
        var connectionString = _containerFixture.GetConnectionString("test_script_code_ok");

        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly<IDependencyMigration>(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DependenciesTests/DependencyScriptMigrations"));
        builder.ConfigureForPostgreSql(connectionString);

        builder.UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.SetUpTargetVersion(new MigrationVersion(4));

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
            new(3),
            new(4)
        };
        Assert.True(result.IsSuccessfully);
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }
    
    [Fact]
    public async Task Migrate_Script_CodeNotOkDependencies()
    {
        var connectionString = _containerFixture.GetConnectionString("test_code_not_ok");

        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly<IDependencyMigration>(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DependenciesTests/DependencyScriptMigrations"));
        builder.ConfigureForPostgreSql(connectionString);

        builder.UseUpgradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.SetUpTargetVersion(new MigrationVersion(5));

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
            new(3),
            new(4)
        };
        Assert.True(result.ErrorCode == MigrationErrorCode.MigratingError);
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }
    
    [Fact]
    public async Task Migrate_Script_ScriptNotOkDependencies()
    {
        var connectionString = _containerFixture.GetConnectionString("test_script_not_ok");

        var builder = new MigrationEngineBuilder();
        builder.UseCodeMigrations().FromAssembly<IDependencyMigration>(Assembly.GetExecutingAssembly());
        builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "DependenciesTests/DependencyScriptMigrations"));
        builder.ConfigureForPostgreSql(connectionString);

        builder.UseUpgradeMigrationPolicy(MigrationPolicy.ShortRunningAllowed);
        builder.UseDowngradeMigrationPolicy(MigrationPolicy.AllAllowed);
        builder.SetUpTargetVersion(new MigrationVersion(7));

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
            new(3),
            new(4)
        };
        Assert.True(result.ErrorCode == MigrationErrorCode.MigratingError);
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }
}