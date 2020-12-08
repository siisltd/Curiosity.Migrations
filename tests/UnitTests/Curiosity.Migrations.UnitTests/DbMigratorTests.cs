using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
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
            
            var provider = new Mock<IDbProvider>();
            
            provider
                .Setup(x => x.GetAppliedMigrationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<DbVersion>));
            
            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var migrations = new List<IMigration>(0);
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                MigrationPolicy.Allowed,
                MigrationPolicy.Forbidden,
                null,
                initialDbVersion);

            var result = await migrator.MigrateSafeAsync();

            provider
                .Verify(x => x.SaveAppliedMigrationVersionAsync(It.IsAny<string>(), It.IsAny<DbVersion>(), It.IsAny<CancellationToken>()), Times.Never);
            
            provider
                .Verify(x => x.CreateDatabaseIfNotExistsAsync(It.IsAny<CancellationToken>()), Times.Never);

            provider
                .Verify(x => x.CreateAppliedMigrationsTableIfNotExistsAsync(It.IsAny<CancellationToken>()), Times.Never);
            
            Assert.True(result.IsSuccessfully);
        }
        
        [Fact]
        public async Task MigrateAsync_UpgradeOnSpecifiedTarget_Ok()
        {
            var initialDbVersion = new DbVersion(1,0);
            var targetDbVersion = new DbVersion(1,1);
            var policy = MigrationPolicy.Allowed;
        
            var actualAppliedMigrations = new HashSet<DbVersion>();
            
            var provider = new Mock<IDbProvider>();
        
            provider
                .Setup(x => x.SaveAppliedMigrationVersionAsync(It.IsAny<string>(), It.IsAny<DbVersion>(), It.IsAny<CancellationToken>()))
                .Callback<string, DbVersion, CancellationToken>((name, version, token) => actualAppliedMigrations.Add(version))
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetAppliedMigrationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<DbVersion>));
            
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
            
            var expectedAppliedMigrations = new HashSet<DbVersion>
            {
                migrations[1].Version
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
            Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
        }
        
        [Fact]
        public async Task MigrateAsync_UpgradeOnNotSpecifiedTarget_Ok()
        {
            var initialDbVersion = new DbVersion(1,0);
            var policy = MigrationPolicy.Allowed;
        
            var actualAppliedMigrations = new HashSet<DbVersion>();
            
            var provider = new Mock<IDbProvider>();
        
            provider
                .Setup(x => x.SaveAppliedMigrationVersionAsync(It.IsAny<string>(), It.IsAny<DbVersion>(), It.IsAny<CancellationToken>()))
                .Callback<string, DbVersion, CancellationToken>((name, version, token) => actualAppliedMigrations.Add(version))
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.GetAppliedMigrationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<DbVersion>));
        
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
            
            var expectedAppliedMigrations = new HashSet<DbVersion>
            {
                migrations[1].Version,
                migrations[2].Version
            };
            
            var migrator = new DbMigrator(
                provider.Object, 
                migrations,
                policy,
                policy);
        
            var result = await migrator.MigrateSafeAsync();
            
            Assert.True(result.IsSuccessfully);
            Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
        }
        
        [Fact]
        public async Task MigrateAsync_UpgradeForbidden_Error()
        {
            var initialDbVersion = new DbVersion(1,0);
            var targetDbVersion = new DbVersion(2,0);
            var policy = MigrationPolicy.Forbidden;
        
            var provider = new Mock<IDbProvider>();
        
            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            provider
                .Setup(x => x.GetAppliedMigrationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<DbVersion>));
        
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
        }
        
        [Fact]
        public void MigrateAsync_NotEnoughMigrations_Error()
        {
            var targetDbVersion = new DbVersion(3,0);
            var policy = MigrationPolicy.Allowed;
        
            var provider = new Mock<IDbProvider>();
            
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

            try
            {
                var _ = new DbMigrator(
                    provider.Object, 
                    migrations,
                    policy,
                    policy,
                    null,
                    targetDbVersion);
                Assert.True(false);
            }
            catch (Exception)
            {
                Assert.True(true);
            }
        }
        
        [Fact]
        public async Task MigrateAsync_Downgrade_Ok()
        {
            var targetDbVersion = new DbVersion(1,0);
            var policy = MigrationPolicy.Allowed;
        
            var actualAppliedMigrations = new HashSet<DbVersion>();
            
            var provider = new Mock<IDbProvider>();
        
            provider
                .Setup(x => x.DeleteAppliedMigrationVersionAsync(It.IsAny<DbVersion>(), It.IsAny<CancellationToken>()))
                .Callback<DbVersion, CancellationToken>((version, token) => actualAppliedMigrations.Add(version))
                .Returns(() => Task.CompletedTask);
            
            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IDowngradeMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IDowngradeMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IDowngradeMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            
            var migrations = new List<IMigration>
            {
                firstMigration.Object,
                secondMigration.Object,
                thirdMigration.Object
            };
            
            provider
                .Setup(x => x.GetAppliedMigrationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(migrations.Select(x => x.Version).ToArray() as IReadOnlyCollection<DbVersion>));

            var expectedAppliedMigrations = new HashSet<DbVersion>
            {
                migrations[1].Version,
                migrations[2].Version
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
            Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
            firstMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
            secondMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
            thirdMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact]
        public async Task MigrateAsync_DowngradeForbidden_Error()
        {
            var targetDbVersion = new DbVersion(1,0);
            var policy = MigrationPolicy.Forbidden;
        
            var provider = new Mock<IDbProvider>();
        
            provider
                .Setup(x => x.BeginTransaction())
                .Returns(() => new MockTransaction());
            
            var firstMigration = new Mock<IDowngradeMigration>();
            firstMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 0));
            var secondMigration = new Mock<IDowngradeMigration>();
            secondMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 1));
            var thirdMigration = new Mock<IDowngradeMigration>();
            thirdMigration
                .Setup(x => x.Version)
                .Returns(new DbVersion(1, 2));
            var fourthMigration = new Mock<IDowngradeMigration>();
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
            
            provider
                .Setup(x => x.GetAppliedMigrationVersionAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(migrations.Select(x => x.Version).ToArray() as IReadOnlyCollection<DbVersion>));

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