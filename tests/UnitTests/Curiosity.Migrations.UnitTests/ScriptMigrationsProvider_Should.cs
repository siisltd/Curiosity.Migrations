using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Curiosity.Migrations.UnitTests;

/// <summary>
/// Unit tests for <see cref="ScriptMigrationsProvider"/>
/// </summary>
public class ScriptMigrationsProvider_Should
{
    [Fact]
    public void GetMigrations_FromDirectory_Ok()
    {
        var dbProvider = Mock.Of<IMigrationConnection>();
        var logger = Mock.Of<ILogger>();

        var migrationsProvider = new ScriptMigrationsProvider();
        var path = Path.Combine(Directory.GetCurrentDirectory(), "ScriptsAsFiles");
        migrationsProvider.FromDirectory(path);

        var migrations = migrationsProvider
            .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
            .ToList();
            
        Assert.Equal(5, migrations.Count);
            
        Assert.True(migrations[0] is DowngradeScriptMigration);
        Assert.Equal(new MigrationVersion(1), migrations[0].Version);
        Assert.Equal("comment", migrations[0].Comment);
        Assert.Equal("up", ((DowngradeScriptMigration)migrations[0]).UpScripts[0].Script);
        Assert.Equal("down", ((DowngradeScriptMigration)migrations[0]).DownScripts[0].Script);
            
        Assert.True(migrations[1] is DowngradeScriptMigration);
        Assert.Equal(new MigrationVersion(1,1), migrations[1].Version);
        Assert.True(String.IsNullOrEmpty(migrations[1].Comment));
        Assert.Equal("up", ((DowngradeScriptMigration)migrations[1]).UpScripts[0].Script);
        Assert.Equal("down", ((DowngradeScriptMigration)migrations[1]).DownScripts[0].Script);

        Assert.True(migrations[2] is ScriptMigration);
        Assert.Equal(new MigrationVersion(1,2), migrations[2].Version);
        Assert.Equal("comment", migrations[2].Comment);
        Assert.Equal("up", ((ScriptMigration)migrations[2]).UpScripts[0].Script);
            
        Assert.True(migrations[3] is ScriptMigration);
        Assert.Equal(new MigrationVersion(1,3), migrations[3].Version);
        Assert.True(String.IsNullOrEmpty(migrations[3].Comment));
        Assert.Equal("up", ((ScriptMigration)migrations[3]).UpScripts[0].Script);
    }

    [Fact]
    public void GetMigrations_FromAssembly_Ok()
    {
        var dbProvider = Mock.Of<IMigrationConnection>();
        var logger = Mock.Of<ILogger>();

        var migrationsProvider = new ScriptMigrationsProvider();
        migrationsProvider.FromAssembly(
            Assembly.GetExecutingAssembly(),
            "Curiosity.Migrations.UnitTests.ScriptsAsResources.Main");

        var migrations = migrationsProvider
            .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
            .ToList();
            
        Assert.Equal(4, migrations.Count);
            
        Assert.True(migrations[0] is DowngradeScriptMigration);
        Assert.Equal(new MigrationVersion(1), migrations[0].Version);
        Assert.Equal("comment", migrations[0].Comment);
        Assert.Equal("up", ((DowngradeScriptMigration)migrations[0]).UpScripts[0].Script);
        Assert.Equal("down", ((DowngradeScriptMigration)migrations[0]).DownScripts[0].Script);
            
            
        Assert.True(migrations[1] is DowngradeScriptMigration);
        Assert.Equal(new MigrationVersion(1,1), migrations[1].Version);
        Assert.True(String.IsNullOrEmpty(migrations[1].Comment));
        Assert.Equal("up", ((DowngradeScriptMigration)migrations[1]).UpScripts[0].Script);
        Assert.Equal("down", ((DowngradeScriptMigration)migrations[1]).DownScripts[0].Script);
            
            
        Assert.True(migrations[2] is ScriptMigration);
        Assert.Equal(new MigrationVersion(1,2), migrations[2].Version);
        Assert.Equal("comment", migrations[2].Comment);
        Assert.Equal("up", ((ScriptMigration)migrations[2]).UpScripts[0].Script);
            
        Assert.True(migrations[3] is ScriptMigration);
        Assert.Equal(new MigrationVersion(1,3), migrations[3].Version);
        Assert.True(String.IsNullOrEmpty(migrations[3].Comment));
        Assert.Equal("up", ((ScriptMigration)migrations[3]).UpScripts[0].Script);
    }
       
    [Fact]
    public void SubstituteVariableToTemplate()
    {
        // arrange
        var dbProvider = Mock.Of<IMigrationConnection>();
        var logger = Mock.Of<ILogger>();

        var migrationsProvider = new ScriptMigrationsProvider();
        var path = Path.Combine(Directory.GetCurrentDirectory(), "ScriptsAsFiles");
        migrationsProvider.FromDirectory(path);

        var userName = "user";
        var variables = new Dictionary<string, string>
        {
            {DefaultVariables.User, userName}
        };
            
        // act
        var migrations = migrationsProvider
            .GetMigrations(dbProvider, variables, logger)
            .ToList();
            
        // assert
        migrations.Count.Should().Be(5, "because we have 5 migrations in scripts directory");

        ((ScriptMigration) migrations[4]).UpScripts[0].Script.Should()
            .BeEquivalentTo(userName, "because script contains only template");
    }


    [Fact]
    public void Should_ThrowException_BecauseOfIncorrectNaming()
    {
        var dbProvider = Mock.Of<IMigrationConnection>();
        var logger = Mock.Of<ILogger>();

        var migrationsProvider = new ScriptMigrationsProvider();
        migrationsProvider.FromAssembly(
            typeof(ScriptConstants).Assembly,
            "Curiosity.Migrations.UnitTests.ScriptsAsResources.IncorrectNamingTest",
            ScriptIncorrectNamingAction.ThrowException);

        Assert.Throws<MigrationException>(() =>
        {
            var _ = migrationsProvider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
        });
    }
}
