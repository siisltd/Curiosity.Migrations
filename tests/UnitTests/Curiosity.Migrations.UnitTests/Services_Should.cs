using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Curiosity.Migrations.UnitTests.CodeMigrations;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Curiosity.Migrations.UnitTests
{
    /// <summary>
    /// Unis test for check adding migration to <see cref="IServiceCollection"/>
    /// </summary>
    public class Services_Should
    {
        /// <summary>
        /// Checks registering single migrator to IoC
        /// </summary>
        [Fact]
        public void AddMigrationToService()
        {
            // arrange

            var services = new ServiceCollection();
            services.AddTransient<DependencyService>();
            services.AddMigrations(options =>
            {
                options
                    .UseScriptMigrations()
                    .FromAssembly(Assembly.GetExecutingAssembly());
                options
                    .UserDbProviderFactory(CreateDbProviderFactory());
            });
            
            // act
            var serviceProvider = services.BuildServiceProvider();
            var migrator = serviceProvider.GetRequiredService<IDbMigrator>();
            
            // assert

            migrator.Should().NotBeNull("because we've registered it");
        }

        private static IDbProviderFactory CreateDbProviderFactory()
        {
            var provider = new Mock<IDbProvider>();
            provider
                .Setup(x => x.GetDefaultVariables())
                .Returns(new Dictionary<string, string>());
            var providerFactory = new Mock<IDbProviderFactory>();
            providerFactory
                .Setup(x => x.CreateDbProvider())
                .Returns(provider.Object);

            return providerFactory.Object;
        }
        
        /// <summary>
        /// Checks registering many migrators to IoC
        /// </summary>
        [Fact]
        public void AddDifferentMigrationToService()
        {
            // arrange
            
            var services = new ServiceCollection();      
            services.AddTransient<DependencyService>();

            services.AddMigrations(options =>
            {
                options
                    .UseScriptMigrations()
                    .FromAssembly(Assembly.GetExecutingAssembly());
                options
                    .UserDbProviderFactory(CreateDbProviderFactory());
            });

            services.AddMigrations(options =>
            {
                options
                    .UseCodeMigrations()
                    .FromAssembly(Assembly.GetExecutingAssembly());
                options
                    .UserDbProviderFactory(CreateDbProviderFactory());
            });
            
            // act
            var serviceProvider = services.BuildServiceProvider();
            var migrators = serviceProvider.GetServices<IDbMigrator>()?.ToList();
            
            // assert

            migrators.Should().NotBeNull("because we've registered it");
            migrators.Count.Should().Be(2, "because we've registered 2 migrations'");
        }
    }
}