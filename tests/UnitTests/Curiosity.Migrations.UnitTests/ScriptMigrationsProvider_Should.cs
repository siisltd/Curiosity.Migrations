using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Curiosity.Migrations.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="ScriptMigrationsProvider"/>
    /// </summary>
    public class ScriptMigrationsProvider_Should
    {
        [Fact]
        public void GetMigrations_FromDirectory_WithoutPrefix_Ok()
        {
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var migrationsProvider = new ScriptMigrationsProvider();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts");
            migrationsProvider.FromDirectory(path);

            var migrations = migrationsProvider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            Assert.Equal(5, migrations.Count);
            
            Assert.True(migrations[0] is ScriptMigration);
            Assert.Equal(new DbVersion(1,0), migrations[0].Version);
            Assert.Equal("comment", migrations[0].Comment);
            Assert.Equal("up", ((ScriptMigration)migrations[0]).UpScript);
            Assert.Equal("down", ((ScriptMigration)migrations[0]).DownScript);
            
            
            Assert.True(migrations[1] is ScriptMigration);
            Assert.Equal(new DbVersion(1,1), migrations[1].Version);
            Assert.True(String.IsNullOrEmpty(migrations[1].Comment));
            Assert.Equal("up", ((ScriptMigration)migrations[1]).UpScript);
            Assert.Equal("down", ((ScriptMigration)migrations[1]).DownScript);
            
            
            Assert.True(migrations[2] is ScriptMigration);
            Assert.Equal(new DbVersion(1,2), migrations[2].Version);
            Assert.Equal("comment", migrations[2].Comment);
            Assert.Equal("up", ((ScriptMigration)migrations[2]).UpScript);
            Assert.True(String.IsNullOrEmpty(((ScriptMigration)migrations[2]).DownScript));
            
            Assert.True(migrations[3] is ScriptMigration);
            Assert.Equal(new DbVersion(1,3), migrations[3].Version);
            Assert.True(String.IsNullOrEmpty(migrations[3].Comment));
            Assert.Equal("up", ((ScriptMigration)migrations[3]).UpScript);
            Assert.True(String.IsNullOrEmpty(((ScriptMigration)migrations[3]).DownScript));
        }
        
        [Fact]
        public void GetMigrations_FromDirectory_WithPrefix_Ok()
        {
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var migrationsProvider = new ScriptMigrationsProvider();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts");
            migrationsProvider.FromDirectory(path, "prefix");

            var migrations = migrationsProvider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            Assert.Equal(1, migrations.Count);
            
            Assert.True(migrations[0] is ScriptMigration);
            Assert.Equal(new DbVersion(0,1), migrations[0].Version);
            Assert.True(String.IsNullOrEmpty(migrations[0].Comment));
            Assert.Equal("prefix", ((ScriptMigration)migrations[0]).UpScript);
            Assert.True(String.IsNullOrEmpty(((ScriptMigration)migrations[0]).DownScript));
        }
        
        [Fact]
        public void GetMigrations_FromAssembly_WithoutPrefix_Ok()
        {
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var migrationsProvider = new ScriptMigrationsProvider();
            migrationsProvider.FromAssembly(Assembly.GetExecutingAssembly());

            var migrations = migrationsProvider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            Assert.Equal(5, migrations.Count);
            
            Assert.True(migrations[0] is ScriptMigration);
            Assert.Equal(new DbVersion(1,0), migrations[0].Version);
            Assert.Equal("comment", migrations[0].Comment);
            Assert.Equal("up", ((ScriptMigration)migrations[0]).UpScript);
            Assert.Equal("down", ((ScriptMigration)migrations[0]).DownScript);
            
            
            Assert.True(migrations[1] is ScriptMigration);
            Assert.Equal(new DbVersion(1,1), migrations[1].Version);
            Assert.True(String.IsNullOrEmpty(migrations[1].Comment));
            Assert.Equal("up", ((ScriptMigration)migrations[1]).UpScript);
            Assert.Equal("down", ((ScriptMigration)migrations[1]).DownScript);
            
            
            Assert.True(migrations[2] is ScriptMigration);
            Assert.Equal(new DbVersion(1,2), migrations[2].Version);
            Assert.Equal("comment", migrations[2].Comment);
            Assert.Equal("up", ((ScriptMigration)migrations[2]).UpScript);
            Assert.True(String.IsNullOrEmpty(((ScriptMigration)migrations[2]).DownScript));
            
            Assert.True(migrations[3] is ScriptMigration);
            Assert.Equal(new DbVersion(1,3), migrations[3].Version);
            Assert.True(String.IsNullOrEmpty(migrations[3].Comment));
            Assert.Equal("up", ((ScriptMigration)migrations[3]).UpScript);
            Assert.True(String.IsNullOrEmpty(((ScriptMigration)migrations[3]).DownScript));
            
            
            Assert.True(migrations[4] is ScriptMigration);
            Assert.Equal(new DbVersion(2,0), migrations[4].Version);
            Assert.True(String.IsNullOrEmpty(migrations[4].Comment));
            Assert.Equal("prefix", ((ScriptMigration)migrations[4]).UpScript);
            Assert.True(String.IsNullOrEmpty(((ScriptMigration)migrations[4]).DownScript));
        }
        
        [Fact]
        public void GetMigrations_FromAssembly_WithPrefix_Ok()
        {
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var migrationsProvider = new ScriptMigrationsProvider();
            migrationsProvider.FromAssembly(Assembly.GetExecutingAssembly(), "PreMigration");

            var migrations = migrationsProvider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            Assert.Equal(1, migrations.Count);
            
            Assert.True(migrations[0] is ScriptMigration);
            Assert.Equal(new DbVersion(2,0), migrations[0].Version);
            Assert.True(String.IsNullOrEmpty(migrations[0].Comment));
            Assert.Equal("prefix", ((ScriptMigration)migrations[0]).UpScript);
            Assert.True(String.IsNullOrEmpty(((ScriptMigration)migrations[0]).DownScript));
        }
        
        [Fact]
        public void SubstituteVariableToTemplate()
        {
            // arrange
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var migrationsProvider = new ScriptMigrationsProvider();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Scripts");
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

            ((ScriptMigration) migrations[4]).UpScript.Should()
                .BeEquivalentTo(userName, "because script contains only template");
        }
    }
}