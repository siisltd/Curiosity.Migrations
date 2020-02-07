using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Curiosity.Migrations.UnitTests
{
    public class DbMigratorTests
    {
        [Fact]
        public async Task MigrateAsync_SkipMigration_Ok()
        {
            var initialDbVersion = new DbVersion(1,0);
            var policy = MigrationPolicy.Major | MigrationPolicy.Minor;
            
            var provider = new Mock<IDbProvider>();
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());

            var migrations = new List<IMigration>(0);
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                initialDbVersion);

            var result = await migrator.MigrateSafeAsync();

            provider
                .Verify(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()), Times.Never);
            
            provider
                .Verify(x => x.CreateDatabaseIfNotExistsAsync(), Times.Once);

            provider
                .Verify(x => x.CreateHistoryTableIfNotExistsAsync(), Times.Never);
            
            Assert.True(result.IsSuccessfully);
        }
        
        [Fact]
        public async Task MigrateAsync_UpgradeOnSpecifiedTarget_Ok()
        {
            var initialDbVersion = new DbVersion(1,0);
            var targetDbVersion = new DbVersion(1,2);
            var policy = MigrationPolicy.Major | MigrationPolicy.Minor;

            var dbVersionAfterUpdate = initialDbVersion;
            
            var provider = new Mock<IDbProvider>();

            provider
                .Setup(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()))
                .Callback<DbVersion>(version => dbVersionAfterUpdate = version)
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));
            
            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                targetDbVersion);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.True(result.IsSuccessfully);
            Assert.Equal(targetDbVersion, dbVersionAfterUpdate);
        }
        
        [Fact]
        public async Task MigrateAsync_UpgradeOnNotSpecifiedTarget_Ok()
        {
            var initialDbVersion = new DbVersion(1,0);
            var policy = MigrationPolicy.All;

            var dbVersionAfterUpdate = initialDbVersion;
            
            var provider = new Mock<IDbProvider>();

            provider
                .Setup(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()))
                .Callback<DbVersion>(version => dbVersionAfterUpdate = version)
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.True(result.IsSuccessfully);
            Assert.Equal(migrations.Max(x => x.Version), dbVersionAfterUpdate);
        }
        
        [Fact]
        public async Task MigrateAsync_UpgradeMajorForbidden_Error()
        {
            var initialDbVersion = new DbVersion(1,0);
            var targetDbVersion = new DbVersion(2,0);
            var policy = MigrationPolicy.Minor;

            var dbVersionAfterUpdate = initialDbVersion;
            
            var provider = new Mock<IDbProvider>();

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            provider
                .Setup(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()))
                .Callback<DbVersion>(version => dbVersionAfterUpdate = version)
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            var fourthMigration = new Mock<IMigration>();
            fourthMigration 
                .Setup(x => x.Version)
                .Returns(new DbVersion(2, 0));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object,
                fourthMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                targetDbVersion);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.False(result.IsSuccessfully);
            Assert.True(result.Error.HasValue);
            Assert.Equal(MigrationError.PolicyError, result.Error.Value);
            Assert.Equal(thirdMigration.Object.Version, dbVersionAfterUpdate);
        }
        
        [Fact]
        public async Task MigrateAsync_UpgradeMinorForbidden_Error()
        {
            var initialDbVersion = new DbVersion(1,0);
            var targetDbVersion = new DbVersion(2,0);
            var policy = MigrationPolicy.Major;

            var dbVersionAfterUpdate = initialDbVersion;
            
            var provider = new Mock<IDbProvider>();

            provider
                .Setup(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()))
                .Callback<DbVersion>(version => dbVersionAfterUpdate = version)
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            var fourthMigration = new Mock<IMigration>();
            fourthMigration 
                .Setup(x => x.Version)
                .Returns(new DbVersion(2, 0));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object,
                fourthMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                targetDbVersion);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.False(result.IsSuccessfully);
            Assert.True(result.Error.HasValue);
            Assert.Equal(MigrationError.PolicyError, result.Error.Value);
            Assert.Equal(initialDbVersion, dbVersionAfterUpdate);
        }
        
        [Fact]
        public async Task MigrateAsync_NotEnoughMigrations_Error()
        {
            var initialDbVersion = new DbVersion(1,0);
            var targetDbVersion = new DbVersion(3,0);
            var policy = MigrationPolicy.All;

            var provider = new Mock<IDbProvider>();
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            var fourthMigration = new Mock<IMigration>();
            fourthMigration 
                .Setup(x => x.Version)
                .Returns(new DbVersion(2, 0));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object,
                fourthMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                targetDbVersion);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.False(result.IsSuccessfully);
            Assert.True(result.Error.HasValue);
            Assert.Equal(MigrationError.MigrationNotFound, result.Error.Value);
        }
        
        [Fact]
        public async Task MigrateAsync_Downgrade_Ok()
        {
            var targetDbVersion = new DbVersion(1,0);
            var initialDbVersion = new DbVersion(1,2);
            var policy = MigrationPolicy.All;

            var dbVersionAfterUpdate = initialDbVersion;
            
            var provider = new Mock<IDbProvider>();

            provider
                .Setup(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()))
                .Callback<DbVersion>(version => dbVersionAfterUpdate = version)
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                targetDbVersion);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.True(result.IsSuccessfully);
            Assert.Equal(targetDbVersion, dbVersionAfterUpdate);
            firstMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>()), Times.Never);
            secondMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>()), Times.Once);
            thirdMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>()), Times.Once);
        }
        
        [Fact]
        public async Task MigrateAsync_DowngradeMajorForbidden_Error()
        {
            var initialDbVersion = new DbVersion(2,0);
            var targetDbVersion = new DbVersion(1,0);
            var policy = MigrationPolicy.Minor;

            var dbVersionAfterUpdate = initialDbVersion;
            
            var provider = new Mock<IDbProvider>();

            provider
                .Setup(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()))
                .Callback<DbVersion>(version => dbVersionAfterUpdate = version)
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            var fourthMigration = new Mock<IMigration>();
            fourthMigration 
                .Setup(x => x.Version)
                .Returns(new DbVersion(2, 0));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object,
                fourthMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                targetDbVersion);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.False(result.IsSuccessfully);
            Assert.True(result.Error.HasValue);
            Assert.Equal(MigrationError.PolicyError, result.Error.Value);
            Assert.Equal(fourthMigration.Object.Version, dbVersionAfterUpdate);
        }
        
        [Fact]
        public async Task MigrateAsync_DowngradeMinorForbidden_Error()
        {
            var initialDbVersion = new DbVersion(2,0);
            var targetDbVersion = new DbVersion(1,0);
            var policy = MigrationPolicy.Major;

            var dbVersionAfterUpdate = initialDbVersion;
            
            var provider = new Mock<IDbProvider>();

            provider
                .Setup(x => x.UpdateCurrentDbVersionAsync(It.IsAny<DbVersion>()))
                .Callback<DbVersion>(version => dbVersionAfterUpdate = version)
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetDbVersionSafeAsync())
                .Returns(() => Task.FromResult(new DbVersion?(initialDbVersion)));

            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            var fourthMigration = new Mock<IMigration>();
            fourthMigration 
                .Setup(x => x.Version)
                .Returns(new DbVersion(2, 0));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object,
                fourthMigration.Object
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy,
                null,
                targetDbVersion);

            var result = await migrator.MigrateSafeAsync();
            
            Assert.False(result.IsSuccessfully);
            Assert.True(result.Error.HasValue);
            Assert.Equal(MigrationError.PolicyError, result.Error.Value);
            Assert.Equal(thirdMigration.Object.Version, dbVersionAfterUpdate);
        }
        
        private class MockTransaction : DbTransaction
        {
            public override void Commit()
            {
            }

            public override void Rollback()
            {
            }

            protected override DbConnection DbConnection { get; }
            public override IsolationLevel IsolationLevel { get; }
        }
    }
    
}