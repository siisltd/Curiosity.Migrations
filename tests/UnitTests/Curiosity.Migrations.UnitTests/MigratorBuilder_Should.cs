using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Curiosity.Migrations.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="MigratorBuilder"/>
    /// </summary>
    public class MigratorBuilder_Should
    {
        private const string ProviderUserName = "user";
        private const string ProviderDbName = "db";
        private const string AdditionalVariableName = "add_var";
        private const string AdditionalVariableValue = "value";
        private const string ManualUserName = "manual user";
        
        [Fact]
        public void RetrieveOnlyDefaultVariablesFromProvider()
        {
            // arrange
            var builder = new MigratorBuilder();

            var providerMock = new Mock<IDbProvider>();
            providerMock
                .Setup(x => x.GetDefaultVariables())
                .Returns(() => new Dictionary<string, string>
                {
                    {DefaultVariables.User, ProviderUserName},
                    {DefaultVariables.DbName, ProviderDbName}
                });
            var factoryMock = new Mock<IDbProviderFactory>();
            factoryMock
                .Setup(x => x.CreateDbProvider())
                .Returns(() => providerMock.Object);

            IReadOnlyDictionary<string, string> scriptVariables = null;
            var migrationsProviderMock = new Mock<IMigrationsProvider>();
            migrationsProviderMock
                .Setup(x => x.GetMigrations(
                    It.IsAny<IDbProvider>(), 
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<ILogger>()))
                .Callback<IDbProvider, IReadOnlyDictionary<string, string>, ILogger>((provider, variables, logger) => { scriptVariables = variables; })
                .Returns<IDbProvider, IReadOnlyDictionary<string, string>, ILogger>((provider, variables, logger) => new List<IMigration>(0));

            builder.UseCustomMigrationsProvider(migrationsProviderMock.Object);
            builder.UserDbProviderFactory(factoryMock.Object);

            // act
            var migrator = builder.Build();
            
            // assert
            migrator.Should().NotBeNull("because we've create migrator");

            scriptVariables.Should().NotBeNull("because provider returns 2 variables");
            scriptVariables.Count.Should().Be(2, "because provider returns variable with user and db names");
            scriptVariables.ContainsKey(DefaultVariables.User).Should().BeTrue("because user name is default variable");
            scriptVariables[DefaultVariables.User].Should().BeEquivalentTo(ProviderUserName, "because we set it manually to mock");
            scriptVariables.ContainsKey(DefaultVariables.DbName).Should().BeTrue("because db name is default variable");
            scriptVariables[DefaultVariables.DbName].Should().BeEquivalentTo(ProviderDbName, "because we set it manually to mock");
        }
        
        [Fact]
        public void OverrideDefaultVariablesFromProvider()
        {
            // arrange
            var builder = new MigratorBuilder();

            var providerMock = new Mock<IDbProvider>();
            providerMock
                .Setup(x => x.GetDefaultVariables())
                .Returns(() => new Dictionary<string, string>
                {
                    {DefaultVariables.User, ProviderUserName},
                    {DefaultVariables.DbName, ProviderDbName}
                });
            var factoryMock = new Mock<IDbProviderFactory>();
            factoryMock
                .Setup(x => x.CreateDbProvider())
                .Returns(() => providerMock.Object);

            IReadOnlyDictionary<string, string> scriptVariables = null;
            var migrationsProviderMock = new Mock<IMigrationsProvider>();
            migrationsProviderMock
                .Setup(x => x.GetMigrations(
                    It.IsAny<IDbProvider>(), 
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<ILogger>()))
                .Callback<IDbProvider, IReadOnlyDictionary<string, string>, ILogger>((provider, variables, logger) => { scriptVariables = variables; })
                .Returns<IDbProvider, IReadOnlyDictionary<string, string>, ILogger>((provider, variables, logger) => new List<IMigration>(0));

            builder.UseCustomMigrationsProvider(migrationsProviderMock.Object);
            builder.UserDbProviderFactory(factoryMock.Object);
            builder.UseVariable(DefaultVariables.User, ManualUserName);
            
            // act
            var migrator = builder.Build();
            
            // assert
            migrator.Should().NotBeNull("because we've create migrator");

            scriptVariables.Should().NotBeNull("because provider returns 2 variables");
            scriptVariables.Count.Should().Be(2, "because provider returns variable with user and db names");
            scriptVariables.ContainsKey(DefaultVariables.User).Should().BeTrue("because user name is default variable");
            scriptVariables[DefaultVariables.User].Should().BeEquivalentTo(ManualUserName, "because we set it manually to builder");
            scriptVariables.ContainsKey(DefaultVariables.DbName).Should().BeTrue("because db name is default variable");
            scriptVariables[DefaultVariables.DbName].Should().BeEquivalentTo(ProviderDbName, "because we set it manually to mock");
        }
        
        [Fact]
        public void RetrieveDefaultVariablesFromProviderWithManual()
        {
            // arrange
            var builder = new MigratorBuilder();

            var providerMock = new Mock<IDbProvider>();
            providerMock
                .Setup(x => x.GetDefaultVariables())
                .Returns(() => new Dictionary<string, string>
                {
                    {DefaultVariables.User, ProviderUserName},
                    {DefaultVariables.DbName, ProviderDbName}
                });
            var factoryMock = new Mock<IDbProviderFactory>();
            factoryMock
                .Setup(x => x.CreateDbProvider())
                .Returns(() => providerMock.Object);

            IReadOnlyDictionary<string, string> scriptVariables = null;
            var migrationsProviderMock = new Mock<IMigrationsProvider>();
            migrationsProviderMock
                .Setup(x => x.GetMigrations(
                    It.IsAny<IDbProvider>(),
                    It.IsAny<Dictionary<string, string>>(),
                    It.IsAny<ILogger>()))
                .Callback<IDbProvider, IReadOnlyDictionary<string, string>, ILogger>((provider, variables, logger) => { scriptVariables = variables; })
                .Returns<IDbProvider, IReadOnlyDictionary<string, string>, ILogger>((provider, variables, logger) => new List<IMigration>(0));

            builder.UseCustomMigrationsProvider(migrationsProviderMock.Object);
            builder.UserDbProviderFactory(factoryMock.Object);
            builder.UseVariable(AdditionalVariableName, AdditionalVariableValue);
                
            // act
            var migrator = builder.Build();
            
            // assert
            migrator.Should().NotBeNull("because we've create migrator");

            scriptVariables.Should().NotBeNull("because provider returns 2 variables");
            scriptVariables.Count.Should().Be(3, "because provider returns variable with user and db names and additional variable");
            scriptVariables.ContainsKey(AdditionalVariableName).Should().BeTrue("because db name is default variable");
            scriptVariables[AdditionalVariableName].Should().BeEquivalentTo(AdditionalVariableValue, "because we set it manually to builder");
        }
        
    }
    
    
}