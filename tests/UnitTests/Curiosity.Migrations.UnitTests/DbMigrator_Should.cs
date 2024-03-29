using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Curiosity.Migrations.UnitTests;

/// <summary>
/// Positive unit tests for <see cref="MigrationEngine"/>.
/// </summary>
public class DbMigrator_Should
{
    #region Upgrade

    [Fact]
    public async Task SkipMigration_On_MigrateAsync()
    {
        var initialDbVersion = new MigrationVersion(1);

        var provider = new Mock<IMigrationConnection>();

        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new[] { initialDbVersion } as IReadOnlyCollection<MigrationVersion>));

        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());

        var migrator = new MigrationEngine(
            provider.Object,
            new List<IMigration>(0),
            MigrationPolicy.AllAllowed,
            MigrationPolicy.AllForbidden,
            null,
            initialDbVersion);

        var result = await migrator.UpgradeDatabaseAsync();

        provider.Verify(x => x.SaveAppliedMigrationVersionAsync(
                It.IsAny<MigrationVersion>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        provider.Verify(x => x.CreateDatabaseIfNotExistsAsync(It.IsAny<CancellationToken>()), Times.Never);

        provider.Verify(x => x.CreateMigrationHistoryTableIfNotExistsAsync(It.IsAny<CancellationToken>()), Times.Never);

        Assert.True(result.IsSuccessfully);
    }

    [Fact]
    public async Task UpgradeToSpecifiedTarget_On_MigrateAsync()
    {
        var initialDbVersion = new MigrationVersion(1);
        var targetDbVersion = new MigrationVersion(1,1);
        var policy = MigrationPolicy.AllAllowed;
        
        var actualAppliedMigrations = new HashSet<MigrationVersion>();
            
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.SaveAppliedMigrationVersionAsync(It.IsAny<MigrationVersion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<MigrationVersion, string, CancellationToken>((version, _, _) => actualAppliedMigrations.Add(version))
            .Returns(() => Task.CompletedTask);
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<MigrationVersion>));
            
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        var firstMigration = new Mock<IMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        firstMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var secondMigration = new Mock<IMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        secondMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var thirdMigration = new Mock<IMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        thirdMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object
        };
            
        var expectedAppliedMigrations = new HashSet<MigrationVersion>
        {
            migrations[1].Version
        };
            
        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            targetDbVersion);
        
        var result = await migrator.UpgradeDatabaseAsync();
            
        Assert.True(result.IsSuccessfully);
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }
        
    [Fact]
    public async Task UpgradeWithNotSpecifiedTarget_On_MigrateAsync()
    {
        var initialDbVersion = new MigrationVersion(1);
        var policy = MigrationPolicy.AllAllowed;
        
        var actualAppliedMigrations = new HashSet<MigrationVersion>();
            
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.SaveAppliedMigrationVersionAsync(It.IsAny<MigrationVersion>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<MigrationVersion, string, CancellationToken>((version, _, _) => actualAppliedMigrations.Add(version))
            .Returns(() => Task.CompletedTask);
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<MigrationVersion>));
        
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        var firstMigration = new Mock<IMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        firstMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var secondMigration = new Mock<IMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        secondMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var thirdMigration = new Mock<IMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        thirdMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object
        };
            
        var expectedAppliedMigrations = new HashSet<MigrationVersion>
        {
            migrations[1].Version,
            migrations[2].Version
        };
            
        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy);
        
        var result = await migrator.UpgradeDatabaseAsync();
            
        Assert.True(result.IsSuccessfully);
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
    }

    [Fact]
    public async Task ForbidMigration_On_MigrateAsync()
    {
        var initialDbVersion = new MigrationVersion(1);
        var targetDbVersion = new MigrationVersion(2);
        var policy = MigrationPolicy.AllForbidden;
        
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<MigrationVersion>));
        
        var firstMigration = new Mock<IMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        var secondMigration = new Mock<IMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        var thirdMigration = new Mock<IMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        var fourthMigration = new Mock<IMigration>();
        fourthMigration 
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(2));
            
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object,
            fourthMigration.Object
        };
            
        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            targetDbVersion);
        
        var result = await migrator.UpgradeDatabaseAsync();
            
        Assert.False(result.IsSuccessfully);
        Assert.True(result.ErrorCode.HasValue);
        Assert.Equal(MigrationErrorCode.PolicyError, result.ErrorCode.Value);
    }
    
    [Fact]
    public async Task SkipLongRunningMigration_On_MigrateAsync()
    {
        var initialDbVersion = new MigrationVersion(1);
        var targetDbVersion = new MigrationVersion(2);
        var policy = MigrationPolicy.ShortRunningAllowed;
        
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<MigrationVersion>));
        
        var firstMigration = new Mock<IMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        firstMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var secondMigration = new Mock<IMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        secondMigration
            .Setup(x => x.IsLongRunning)
            .Returns(true);
        secondMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var thirdMigration = new Mock<IMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        thirdMigration
            .Setup(x => x.IsLongRunning)
            .Returns(true);
        thirdMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var fourthMigration = new Mock<IMigration>();
        fourthMigration 
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(2));
        fourthMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object,
            fourthMigration.Object
        };
            
        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            targetDbVersion);
        
        var result = await migrator.UpgradeDatabaseAsync();

        var notAppliedMigrations = migrations.Where(x => x.Version != initialDbVersion).ToList();
        var shortRunningMigrations = new HashSet<MigrationVersion>(notAppliedMigrations.Where(x => !x.IsLongRunning).Select(x => x.Version));
        var longRunningMigrations = new HashSet<MigrationVersion>(notAppliedMigrations.Where(x => x.IsLongRunning).Select(x => x.Version));
        var appliedMigrations = new HashSet<MigrationVersion>(result.AppliedMigrations.Select(x => x.Version));
        var skippedMigrations = new HashSet<MigrationVersion>(result.SkippedByPolicyMigrations.Select(x => x.Version));

        Assert.True(result.IsSuccessfully);
        Assert.False(result.ErrorCode.HasValue);
        Assert.NotEmpty(result.SkippedByPolicyMigrations);
        Assert.Equal(shortRunningMigrations, appliedMigrations);
        Assert.Equal(longRunningMigrations, skippedMigrations);
    }

    [Fact]
    public async Task SkipShortRunningMigration_On_MigrateAsync()
    {
        var initialDbVersion = new MigrationVersion(1);
        var targetDbVersion = new MigrationVersion(2);
        var policy = MigrationPolicy.LongRunningAllowed;
        
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<MigrationVersion>));
        
        var firstMigration = new Mock<IMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        firstMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var secondMigration = new Mock<IMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        secondMigration
            .Setup(x => x.IsLongRunning)
            .Returns(true);
        secondMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var thirdMigration = new Mock<IMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        thirdMigration
            .Setup(x => x.IsLongRunning)
            .Returns(true);
        thirdMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var fourthMigration = new Mock<IMigration>();
        fourthMigration 
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(2));
        fourthMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object,
            fourthMigration.Object
        };
            
        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            targetDbVersion);
        
        var result = await migrator.UpgradeDatabaseAsync();

        var notAppliedMigrations = migrations.Where(x => x.Version != initialDbVersion).ToList();
        var shortRunningMigrations = new HashSet<MigrationVersion>(notAppliedMigrations.Where(x => !x.IsLongRunning).Select(x => x.Version));
        var longRunningMigrations = new HashSet<MigrationVersion>(notAppliedMigrations.Where(x => x.IsLongRunning).Select(x => x.Version));
        var appliedMigrations = new HashSet<MigrationVersion>(result.AppliedMigrations.Select(x => x.Version));
        var skippedMigrations = new HashSet<MigrationVersion>(result.SkippedByPolicyMigrations.Select(x => x.Version));

        Assert.True(result.IsSuccessfully);
        Assert.False(result.ErrorCode.HasValue);
        Assert.NotEmpty(result.SkippedByPolicyMigrations);
        Assert.Equal(shortRunningMigrations, skippedMigrations);
        Assert.Equal(longRunningMigrations, appliedMigrations);
    }
    
    [Fact]
    public async Task ExecutesOnlyTargetLongRunning_On_MigrateAsync()
    {
        var initialDbVersion = new MigrationVersion(1);
        var targetDbVersion = new MigrationVersion(1, 2);
        var policy = MigrationPolicy.LongRunningAllowed;
        
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(new []{initialDbVersion} as IReadOnlyCollection<MigrationVersion>));
        
        var firstMigration = new Mock<IMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        firstMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var secondMigration = new Mock<IMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        secondMigration
            .Setup(x => x.IsLongRunning)
            .Returns(true);
        secondMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var thirdMigration = new Mock<IMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        thirdMigration
            .Setup(x => x.IsLongRunning)
            .Returns(true);
        thirdMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var fourthMigration = new Mock<IMigration>();
        fourthMigration 
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(2));
        fourthMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object,
            fourthMigration.Object
        };
            
        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            targetDbVersion,
            true);
        
        var result = await migrator.UpgradeDatabaseAsync();

        var notAppliedMigrations = migrations.Where(x => x.Version != initialDbVersion).ToList();
        var shortRunningMigrations = new HashSet<MigrationVersion>(notAppliedMigrations.Where(x => !x.IsLongRunning).Select(x => x.Version));
        var longRunningMigrations = new HashSet<MigrationVersion>(
            notAppliedMigrations
                .Where(x => x.IsLongRunning)
                .Where(x => x.Version == targetDbVersion)
                .Select(x => x.Version));
        var appliedMigrations = new HashSet<MigrationVersion>(result.AppliedMigrations.Select(x => x.Version));

        Assert.True(result.IsSuccessfully);
        Assert.False(result.ErrorCode.HasValue);
        Assert.Empty(result.SkippedByPolicyMigrations);
        Assert.Equal(longRunningMigrations, appliedMigrations);
    }
        
    [Fact]
    public void ThrowException_When_NotEnoughMigrations_On_MigrateAsync()
    {
        var targetDbVersion = new MigrationVersion(3);
        var policy = MigrationPolicy.AllAllowed;
        
        var provider = new Mock<IMigrationConnection>();
            
        var firstMigration = new Mock<IMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        var secondMigration = new Mock<IMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        var thirdMigration = new Mock<IMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        var fourthMigration = new Mock<IMigration>();
        fourthMigration 
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(2));
            
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object,
            fourthMigration.Object
        };

        try
        {
            var _ = new MigrationEngine(
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
    public async Task ApplyMigrations_OnPatchManner_On_MigrateAsync()
    {
        var policy = MigrationPolicy.AllAllowed;

        var provider = new Mock<IMigrationConnection>();

        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());

        // files of an applied migrations that are present in a migrator
        var appliedMigrationFilesBase = new List<IMigration>
        {
            GetIMigrationMock("1.0"),

            GetIMigrationMock("202103221906.00"),
            GetIMigrationMock("202103242146.00"),
            GetIMigrationMock("202104022050.00"),
            GetIMigrationMock("202104191546.00"),
            GetIMigrationMock("202104211654.00"),
            GetIMigrationMock("202105050858.00"),
            GetIMigrationMock("202106081017.00"),
            GetIMigrationMock("202106100920.00"),
            GetIMigrationMock("202106241037.00"),
            GetIMigrationMock("202107011038.00"),
            GetIMigrationMock("202107071444.00"),
            GetIMigrationMock("202107161154.00"),
            GetIMigrationMock("202107161116.00"),
            GetIMigrationMock("202107201055.00"),
            GetIMigrationMock("202108031052.00"),
            GetIMigrationMock("202108131242.00"),
            GetIMigrationMock("202108261125.00"),
            GetIMigrationMock("202109031103.00"),
            GetIMigrationMock("202110141456.00"),
            GetIMigrationMock("202110251415.00"),
            GetIMigrationMock("202110221206.00"),
            GetIMigrationMock("202110241625.00"),
            GetIMigrationMock("202110241625.01"),
            GetIMigrationMock("202110241625.02"),

            GetIMigrationMock("202110241625.03"),
            GetIMigrationMock("202110241625.04"),
            GetIMigrationMock("202110241625.05"),
            GetIMigrationMock("202110241625.06"),
            GetIMigrationMock("202110282154.00"),
            GetIMigrationMock("202111021121.00"),
            GetIMigrationMock("202111021121.01"),
            GetIMigrationMock("202111021121.02"),
            GetIMigrationMock("202111111841.00"),
            GetIMigrationMock("202111151316.00"),
            GetIMigrationMock("202111231131.00"),
            GetIMigrationMock("202112041309.00"),
            GetIMigrationMock("202112081626.00"),
            GetIMigrationMock("202112231454.00"),
            GetIMigrationMock("202112061141.00"),
            GetIMigrationMock("202112301840.00"),
            GetIMigrationMock("202112301840.01"),
            GetIMigrationMock("202201091925.00"),
            GetIMigrationMock("202201281033.00"),
        };

        // list of migration that have been already applied to a PDS
        var appliedCommonMigrations =  new List<IMigration>
        {
            GetIMigrationMock("1.1"),
            GetIMigrationMock("1.3"),
            GetIMigrationMock("1.4"),
            GetIMigrationMock("2.0"),
            GetIMigrationMock("2.1"),
            GetIMigrationMock("2.2"),
            GetIMigrationMock("2.3"),
            GetIMigrationMock("2.4"),
            GetIMigrationMock("3.0"),
            GetIMigrationMock("3.1"),
            GetIMigrationMock("3.2"),
            GetIMigrationMock("3.3"),
            GetIMigrationMock("3.4"),
            GetIMigrationMock("4.0"),
            GetIMigrationMock("4.1"),
            GetIMigrationMock("4.2"),

            GetIMigrationMock("4.3"),
            GetIMigrationMock("5.0"),
            GetIMigrationMock("6.0"),
            GetIMigrationMock("6.1"),
            GetIMigrationMock("6.2"),
            GetIMigrationMock("6.3"),
            GetIMigrationMock("6.4"),
            GetIMigrationMock("6.5"),
            GetIMigrationMock("7.0"),
            GetIMigrationMock("8.0"),
            GetIMigrationMock("8.1"),
            GetIMigrationMock("9.0"),
            GetIMigrationMock("10.0"),
            GetIMigrationMock("11.0"),
            GetIMigrationMock("12.0"),
            GetIMigrationMock("13.0"),
            GetIMigrationMock("13.1"),
            GetIMigrationMock("202012092040.00"),
            GetIMigrationMock("202012171439.00"),
            GetIMigrationMock("202012181745.00"),
            GetIMigrationMock("202012221706.00"),
            GetIMigrationMock("202012221706.01"),
            GetIMigrationMock("202012221706.03"),
            GetIMigrationMock("202012222353.00"),
            GetIMigrationMock("202012171517.00"),
            GetIMigrationMock("202012171517.01"),
            GetIMigrationMock("202012221649.00"),
            GetIMigrationMock("202012231913.00"),
            GetIMigrationMock("202012221706.04"),
            GetIMigrationMock("202012221706.05"),
            GetIMigrationMock("202012171531.00"),

            GetIMigrationMock("202012171541.00"),
            GetIMigrationMock("202101121834.00"),
            GetIMigrationMock("202102021500.00"),
            GetIMigrationMock("202102021718.00"),
            GetIMigrationMock("202102030902.00"),
            GetIMigrationMock("202102041810.00"),
            GetIMigrationMock("202102052010.00"),
            GetIMigrationMock("202102120930.00"),
            GetIMigrationMock("202102191244.00"),
        };

        // there are migrations has been applied but not existed in a migration list (because they were added from another branch)
        var appliedButNotExisted = new List<IMigration>
        {
            GetIMigrationMock("202202151428.00"),
            GetIMigrationMock("202202151428.02"),
            GetIMigrationMock("202202151428.03"),
            GetIMigrationMock("202202151428.04"),
            GetIMigrationMock("202202151428.05"),
            GetIMigrationMock("202202151428.06"),
            GetIMigrationMock("202202151428.07"),
            GetIMigrationMock("202202151428.08"),
            GetIMigrationMock("202202151428.09"),
            GetIMigrationMock("202202151428.10"),
            GetIMigrationMock("202202181352.00")
        };

        // there are new migrations we want to apply
        var notAppliedMigrations = new List<IMigration>
        {
            GetIMigrationMock("20220215_1200.00"),
            GetIMigrationMock("20220215_1200.01"),
            GetIMigrationMock("20220215_1200.02")
        };

        var totalAppliedMigrations = new List<IMigration>(appliedMigrationFilesBase);
        totalAppliedMigrations.AddRange(appliedCommonMigrations);
        totalAppliedMigrations.AddRange(appliedButNotExisted);

        var totalMigrationsFiles = new List<IMigration>(appliedMigrationFilesBase);
        totalMigrationsFiles.AddRange(notAppliedMigrations);

        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(totalAppliedMigrations.Select(x => x.Version).ToList() as IReadOnlyCollection<MigrationVersion>));

        var migrator = new MigrationEngine(
            provider.Object,
            totalMigrationsFiles,
            policy,
            policy);

        var result = await migrator.UpgradeDatabaseAsync();

        Assert.True(result.IsSuccessfully);
        Assert.Equal(notAppliedMigrations.Count, result.AppliedMigrations.Count);
    }

    private IMigration GetIMigrationMock(string version)
    {
        var migration = new Mock<IMigration>();
        migration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(version));

        migration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());

        return migration.Object;
    }

    #endregion

    #region Downgrade

    [Fact]
    public async Task MigrateAsync_Downgrade_Ok()
    {
        var targetDbVersion = new MigrationVersion(1);
        var policy = MigrationPolicy.AllAllowed;
        
        var actualAppliedMigrations = new HashSet<MigrationVersion>();
            
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.DeleteAppliedMigrationVersionAsync(It.IsAny<MigrationVersion>(), It.IsAny<CancellationToken>()))
            .Callback<MigrationVersion, CancellationToken>((version, _) => actualAppliedMigrations.Add(version))
            .Returns(() => Task.CompletedTask);
            
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        var firstMigration = new Mock<IDowngradeMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        firstMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var secondMigration = new Mock<IDowngradeMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        secondMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var thirdMigration = new Mock<IDowngradeMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        thirdMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
            
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object
        };
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(migrations.Select(x => x.Version).ToArray() as IReadOnlyCollection<MigrationVersion>));

        var expectedAppliedMigrations = new HashSet<MigrationVersion>
        {
            migrations[1].Version,
            migrations[2].Version
        };
            
        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            targetDbVersion);
        
        var result = await migrator.DowngradeDatabaseAsync();
            
        Assert.True(result.IsSuccessfully);
        Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
        firstMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
        secondMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        thirdMigration.Verify(x => x.DowngradeAsync(It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }
        
    [Fact]
    public async Task MigrateAsync_DowngradeForbidden_Error()
    {
        var targetDbVersion = new MigrationVersion(1);
        var policy = MigrationPolicy.AllForbidden;
        
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        var firstMigration = new Mock<IDowngradeMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        firstMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var secondMigration = new Mock<IDowngradeMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        secondMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var thirdMigration = new Mock<IDowngradeMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        thirdMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        var fourthMigration = new Mock<IDowngradeMigration>();
        fourthMigration 
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(2));
        fourthMigration.Setup(x => x.Dependencies)
            .Returns(new List<MigrationVersion>());
        
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object,
            fourthMigration.Object
        };
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(migrations.Select(x => x.Version).ToArray() as IReadOnlyCollection<MigrationVersion>));

        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            targetDbVersion);
        
        var result = await migrator.DowngradeDatabaseAsync();
            
        Assert.False(result.IsSuccessfully);
        Assert.True(result.ErrorCode.HasValue);
        Assert.Equal(MigrationErrorCode.PolicyError, result.ErrorCode.Value);
    }
    
    [Fact]
    public async Task ThrowException_WhenNoTargetVersion_OnDowngrade()
    {
        var policy = MigrationPolicy.AllForbidden;
        
        var provider = new Mock<IMigrationConnection>();
        
        provider
            .Setup(x => x.BeginTransaction())
            .Returns(() => new MockTransaction());
            
        var firstMigration = new Mock<IDowngradeMigration>();
        firstMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1));
        var secondMigration = new Mock<IDowngradeMigration>();
        secondMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 1));
        var thirdMigration = new Mock<IDowngradeMigration>();
        thirdMigration
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(1, 2));
        var fourthMigration = new Mock<IDowngradeMigration>();
        fourthMigration 
            .Setup(x => x.Version)
            .Returns(new MigrationVersion(2));
            
        var migrations = new List<IMigration>
        {
            firstMigration.Object,
            secondMigration.Object,
            thirdMigration.Object,
            fourthMigration.Object
        };
            
        provider
            .Setup(x => x.GetAppliedMigrationVersionsAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(migrations.Select(x => x.Version).ToArray() as IReadOnlyCollection<MigrationVersion>));

        var migrator = new MigrationEngine(
            provider.Object, 
            migrations,
            policy,
            policy,
            null,
            null);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await migrator.DowngradeDatabaseAsync();
        });
    }

    #endregion

    private class MockTransaction : DbTransaction
    {
        public override void Commit()
        {
        }
        
        public override void Rollback()
        {
        }
        
        // ReSharper disable UnassignedGetOnlyAutoProperty
        protected override DbConnection DbConnection { get; }
        public override IsolationLevel IsolationLevel { get; }
        // ReSharper restore UnassignedGetOnlyAutoProperty
    }
}
