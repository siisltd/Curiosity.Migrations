using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Curiosity.Migrations.UnitTests.CodeMigrations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Curiosity.Migrations.UnitTests
{
    
    /// <summary>
    /// Unit tests for <see cref="CodeMigrationsProvider"/>
    /// </summary>
    public class CodeMigrationsProvider_Should
    {
        /// <summary>
        /// Check if provider returns code migrations from specified assembly
        /// </summary>
        [Fact]
        public void ReturnMigrationsFromAssembly()
        {
            // arrange
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var provider = new CodeMigrationsProvider(GetServiceCollection());
            
            provider.FromAssembly(Assembly.GetExecutingAssembly());

            // act 
            var migrations = provider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            // assert
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

        private IServiceCollection GetServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddTransient<DependencyService>();

            return services;
        }
        
        /// <summary>
        /// Checks if provider returns only code migrations from specified assembly that inherited from desired base class
        /// </summary>
        [Fact]
        public void ReturnCodeMigrationsFromAssemblyByBaseClass()
        {
            // arrange
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var provider = new CodeMigrationsProvider(GetServiceCollection());
            
            provider.FromAssembly<CustomBaseCodeMigration>(Assembly.GetExecutingAssembly());

            // act
            var migrations = provider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            // assert
            Assert.Single(migrations);
            
            Assert.True(migrations[0] is CodeMigration);
            Assert.Equal(new DbVersion(1,3), migrations[0].Version);
            Assert.Equal("comment", migrations[0].Comment);
            
        }
        
        
        /// <summary>
        /// Checks if provider returns only code migrations from specified assembly that implemented desired interface
        /// </summary>
        [Fact]
        public void ReturnCodeMigrationsFromAssemblyByInterface()
        {
            // arrange
            var dbProvider = Mock.Of<IDbProvider>();

            var logger = Mock.Of<ILogger>();
            var provider = new CodeMigrationsProvider(GetServiceCollection());
            
            provider.FromAssembly<ISpecificCodeMigrations>(Assembly.GetExecutingAssembly());

            // act
            var migrations = provider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            // assert
            Assert.Equal(2, migrations.Count);
            
            Assert.True(migrations[0] is CodeMigration);
            Assert.Equal(new DbVersion(1,1), migrations[0].Version);
            Assert.Equal("comment", migrations[0].Comment);
            
            Assert.True(migrations[1] is CodeMigration);
            Assert.Equal(new DbVersion(1,2), migrations[1].Version);
            Assert.Equal("comment", migrations[1].Comment);
            
        }

        [Fact]
        public void ReturnCodeMigrationWithDependenciesFromIoC()
        {
            // arrange
            var dbProvider = Mock.Of<IDbProvider>();
            var logger = Mock.Of<ILogger>();

            var provider = new CodeMigrationsProvider(GetServiceCollection());
            
            provider.FromAssembly(Assembly.GetExecutingAssembly());

            // act
            var migrations = provider
                .GetMigrations(dbProvider, new Dictionary<string, string>(), logger)
                .ToList();
            
            // assert

            migrations.Count.Should().Be(4, "because we have only 4 code migrations");
            var typedMigration = migrations[3] as FourthMigrationWithDependency;
            typedMigration.Should().NotBeNull();
            typedMigration.DependencyService.Should().NotBeNull("because IoC should create dependency");
        }
    }
}