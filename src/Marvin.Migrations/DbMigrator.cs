using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Marvin.Migrations.Migrations;
using Microsoft.Extensions.Logging;

namespace Marvin.Migrations
{
    /// <summary>
    /// Default realisation of <see cref="IDbMigrator"/>
    /// </summary>
    public sealed class DbMigrator : IDbMigrator
    {
        private readonly AutoMigrationPolicy _upgradePolicy;
        private readonly AutoMigrationPolicy _downgradePolicy;

        private readonly ILogger _logger;

        private readonly IDbProvider _dbProvider;
        private readonly List<IMigration> _migrations;

        private readonly DbVersion? _targetVersion;

        /// <summary>
        /// Default realisation of <see cref="IDbMigrator"/>
        /// </summary>
        /// <param name="dbProvider"></param>
        /// <param name="migrations"></param>
        /// <param name="upgradePolicy"></param>
        /// <param name="downgradePolicy"></param>
        /// <param name="targetVersion"></param>
        /// <param name="logger"></param>
        public DbMigrator(
            IDbProvider dbProvider,
            ICollection<IMigration> migrations,
            AutoMigrationPolicy upgradePolicy, 
            AutoMigrationPolicy downgradePolicy,
            DbVersion? targetVersion,
            ILogger logger = null)
        {
            if (migrations == null) throw new ArgumentNullException(nameof(migrations));
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            
            _migrations = migrations.ToList();
            _upgradePolicy = upgradePolicy;
            _downgradePolicy = downgradePolicy;
            _targetVersion = targetVersion;
            _logger = logger;
        }

        public async Task<MigrationResult> MigrateSafeAsync()
        {
            try
            {
                await MigrateAsync();
                return MigrationResult.SuccessfullyResult();
            }
            catch (MigrationException e)
            {
                return MigrationResult.FailureResult(e.Error, e.Message);
            }
            catch (Exception e)
            {
                return MigrationResult.FailureResult(MigrationError.Unknown, e.Message);
            }
        }

        public async Task MigrateAsync()
        {
            try
            {
                await _dbProvider.CreateDatabaseIfNotExistsAsync();
                await _dbProvider.CreateHistoryTableIfNotExistsAsync();
                var dbVersion = await _dbProvider
                                    .GetDbVersionSafeAsync()
                                    .ConfigureAwait(false)
                                ?? new DbVersion?(new DbVersion(0,0));

            
                var targetVersion = _targetVersion ?? _migrations.Max(x => x.Version);
                if (targetVersion == dbVersion.Value)
                {
                    _logger?.LogInformation($"Database {_dbProvider.DbName} is actual. Skip migration.");
                    return;
                }
                
                if (_migrations.All(x => x.Version != targetVersion))
                    throw new MigrationException(MigrationError.MigrationNotFound, $"Migration {targetVersion} not found");
            
                _logger?.LogInformation($"Migrating database {_dbProvider.DbName}...");
                if (targetVersion > dbVersion.Value)
                {
                    _logger?.LogInformation($"Upgrading database {_dbProvider.DbName} from {dbVersion.Value} to {targetVersion}...");
                    await UpgradeAsync(dbVersion.Value);
                    _logger?.LogInformation($"Upgrading database {_dbProvider.DbName} completed.");
                }
                else
                {
                    _logger?.LogInformation($"Downgrading database {_dbProvider.DbName} from {dbVersion.Value} to {targetVersion}...");
                    await DowngradeAsync(dbVersion.Value);
                    _logger?.LogInformation($"Downgrading database {_dbProvider.DbName} completed.");
                }
                _logger?.LogInformation($"Migrating database {_dbProvider.DbName} completed.");
            }
            catch (Exception e)
            {
                _logger?.LogError($"Error while migrating database {_dbProvider.DbName}", e);
                throw;
            }
           
        }

        private async Task UpgradeAsync(DbVersion actualVersion)
        {
            var desiredMigrations = _migrations
                .Where(x => x.Version > actualVersion)
                .OrderBy(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0) return;
            
            var targetVersion = desiredMigrations.Last().Version;
            var lastMigrationVersion = new DbVersion(0,0);
            foreach (var migration in desiredMigrations)
            {
                if (!IsMigrationAllowed(DbVersion.GetDifference(actualVersion, migration.Version), _upgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError, $"Policy restrict upgrade to {migration.Version}. Migration comment: {migration.Comment}");
                }
                _logger.LogInformation($"Upgrading to {migration.Version} (DB {_dbProvider.DbName})...");
                await migration.UpgradeAsync();
                await _dbProvider.UpdateCurrentDbVersionAsync(migration.Version);
                lastMigrationVersion = migration.Version;
                _logger.LogInformation($"Upgrading to {migration.Version} (DB {_dbProvider.DbName}) completed.");
            }
            
            if (lastMigrationVersion != targetVersion) throw new MigrationException(
                MigrationError.MigratingError, 
                $"Can not upgrade database to version {targetVersion}. Last executed migration is {lastMigrationVersion}");
        }
   
        private bool IsMigrationAllowed(DbVersionDifference versionDifference, AutoMigrationPolicy policy)
        {
            switch (versionDifference)
            {
                case DbVersionDifference.Major:
                    return policy.HasFlag(AutoMigrationPolicy.Major);

                case DbVersionDifference.Minor:
                    return policy.HasFlag(AutoMigrationPolicy.Minor);

                default:
                    return false;
            }
        }
        
        private async Task DowngradeAsync(DbVersion actualVersion)
        {
            var desiredMigrations = _migrations
                .Where(x => x.Version > actualVersion)
                .OrderByDescending(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0) return;
            
            var targetVersion = desiredMigrations.Last().Version;
            var lastMigrationVersion = new DbVersion(0,0);
            foreach (var migration in desiredMigrations)
            {
                if (!IsMigrationAllowed(DbVersion.GetDifference(actualVersion, migration.Version), _upgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError, $"Policy restrict downgrade to {migration.Version}. Migration comment: {migration.Comment}");
                }
                _logger.LogInformation($"Downgrade to {migration.Version} (DB {_dbProvider.DbName})...");
                await migration.DowngradeAsync();
                await _dbProvider.UpdateCurrentDbVersionAsync(migration.Version);
                lastMigrationVersion = migration.Version;
                _logger.LogInformation($"Downgrade to {migration.Version} (DB {_dbProvider.DbName}) completed.");
            }
            
            if (lastMigrationVersion != targetVersion) throw new MigrationException(
                MigrationError.MigratingError, 
                $"Can not downgrade database to version {targetVersion}. Last executed migration is {lastMigrationVersion}");
        }
    }
}