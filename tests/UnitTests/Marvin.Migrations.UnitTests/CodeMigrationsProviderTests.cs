using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Marvin.Migrations.UnitTests.CodeMigrations;
using Moq;
using Xunit;

namespace Marvin.Migrations.UnitTests
{
    
    public class CodeMigrationsProviderTests
    {
        [Fact]
        public void GetMigrations_AllFromAssembly_Ok()
        {
            var dbProvider = Mock.Of<IDbProvider>();

            var provider = new CodeMigrationsProvider();
            
            provider.FromAssembly(Assembly.GetExecutingAssembly());

            var migrations = provider.GetMigrations(dbProvider, new Dictionary<string, string>()).ToList();
            
            Assert.Equal(4, migrations.Count);
            
            Assert.True(migrations[0] is CodeMigration);
            Assert.Equal(new DbVersion(1,0), migrations[0].Version);
            Assert.Equal("comment", migrations[0].Comment);
            
            
            Assert.True(migrations[1] is CodeMigration);
            Assert.Equal(new DbVersion(1,1), migrations[1].Version);
            Assert.Equal("comment", migrations[1].Comment);
            
            
            Assert.True(migrations[2] is CodeMigration);
            Assert.Equal(new DbVersion(1,2), migrations[2].Version);
            Assert.Equal("comment", migrations[2].Comment);
            
            Assert.True(migrations[3] is CodeMigration);
            Assert.Equal(new DbVersion(1,3), migrations[3].Version);
            Assert.Equal("comment", migrations[3].Comment);
        }
        
        [Fact]
        public void GetMigrations_CustomByClassFromAssembly_Ok()
        {
            var dbProvider = Mock.Of<IDbProvider>();

            var provider = new CodeMigrationsProvider();
            
            provider.FromAssembly<CustomBaseCodeMigration>(Assembly.GetExecutingAssembly());

            var migrations = provider.GetMigrations(dbProvider, new Dictionary<string, string>()).ToList();
            
            Assert.Single(migrations);
            
            Assert.True(migrations[0] is CodeMigration);
            Assert.Equal(new DbVersion(1,3), migrations[0].Version);
            Assert.Equal("comment", migrations[0].Comment);
            
        }
        
        [Fact]
        public void GetMigrations_CustomByInterfaceFromAssembly_Ok()
        {
            var dbProvider = Mock.Of<IDbProvider>();

            var provider = new CodeMigrationsProvider();
            
            provider.FromAssembly<ISpecificCodeMigrations>(Assembly.GetExecutingAssembly());

            var migrations = provider.GetMigrations(dbProvider, new Dictionary<string, string>()).ToList();
            
            Assert.Equal(2, migrations.Count);
            
            Assert.True(migrations[0] is CodeMigration);
            Assert.Equal(new DbVersion(1,1), migrations[0].Version);
            Assert.Equal("comment", migrations[0].Comment);
            
            Assert.True(migrations[1] is CodeMigration);
            Assert.Equal(new DbVersion(1,2), migrations[1].Version);
            Assert.Equal("comment", migrations[1].Comment);
            
        }
    }
}