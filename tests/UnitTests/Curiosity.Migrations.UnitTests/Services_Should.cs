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

            var provider = new Mock<IDbProvider>();
            var providerFactory = new Mock<IDbProviderFactory>();
            providerFactory
                .Setup(x => x.CreateDbProvider())
                .Returns(provider.Object);
            
            var services = new ServiceCollection();
            services.AddTransient<DependencyService>();
            services.AddMigrations(options =>
            {
                options
                    .UseScriptMigrations()
                    .FromAssembly(Assembly.GetExecutingAssembly());
                options
                    .UserDbProviderFactory(providerFactory.Object);
            });
            
            // act
            var serviceProvider = services.BuildServiceProvider();
            var migrator = serviceProvider.GetRequiredService<IDbMigrator>();
            
            // assert

            migrator.Should().NotBeNull("because we've registered it");
        } 
        
        /// <summary>
        /// Checks registering many migrators to IoC
        /// </summary>
        [Fact]
        public void AddDifferentMigrationToService()
        {
            // arrange

            var provider = new Mock<IDbProvider>();
            var providerFactory = new Mock<IDbProviderFactory>();
            providerFactory
                .Setup(x => x.CreateDbProvider())
                .Returns(provider.Object);
            
            var services = new ServiceCollection();      
            services.AddTransient<DependencyService>();

            services.AddMigrations(options =>
            {
                options
                    .UseScriptMigrations()
                    .FromAssembly(Assembly.GetExecutingAssembly());
                options
                    .UserDbProviderFactory(providerFactory.Object);
            });

            services.AddMigrations(options =>
            {
                options
                    .UseCodeMigrations()
                    .FromAssembly(Assembly.GetExecutingAssembly());
                options
                    .UserDbProviderFactory(providerFactory.Object);
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