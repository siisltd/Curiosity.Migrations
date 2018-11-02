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
    public sealed class DbMigrator : IDbMigrator, IDisposable
    {
        private readonly MigrationPolicy _upgradePolicy;
        private readonly MigrationPolicy _downgradePolicy;

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
            MigrationPolicy upgradePolicy, 
            MigrationPolicy downgradePolicy,
            DbVersion? targetVersion = null,
            ILogger logger = null)
        {
            if (migrations == null) throw new ArgumentNullException(nameof(migrations));
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            
            _migrations = migrations.ToList();
            
            var  migrationCheckMap = new HashSet<DbVersion>();
            foreach (var migration in _migrations)
            {
                if (migrationCheckMap.Contains(migration.Version))
                    throw new InvalidOperationException(
                        $"There is more than one migration with version {migration.Version}");

                migrationCheckMap.Add(migration.Version);
            }
            
            _upgradePolicy = upgradePolicy;
            _downgradePolicy = downgradePolicy;
            _targetVersion = targetVersion;
            _logger = logger;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task MigrateAsync()
        {
            try
            {
                await _dbProvider.OpenConnectionAsync();
                await _dbProvider.CreateDatabaseIfNotExistsAsync();
                await _dbProvider.CreateHistoryTableIfNotExistsAsync();
                var dbVersion = await _dbProvider
                                    .GetDbVersionSafeAsync()
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
                    await UpgradeAsync(dbVersion.Value, targetVersion);
                    _logger?.LogInformation($"Upgrading database {_dbProvider.DbName} completed.");
                }
                else
                {
                    _logger?.LogInformation($"Downgrading database {_dbProvider.DbName} from {dbVersion.Value} to {targetVersion}...");
                    await DowngradeAsync(dbVersion.Value, targetVersion);
                    _logger?.LogInformation($"Downgrading database {_dbProvider.DbName} completed.");
                }

                await _dbProvider.CloseConnectionAsync();
                _logger?.LogInformation($"Migrating database {_dbProvider.DbName} completed.");
            }
            catch (Exception e)
            {
                _logger?.LogError($"Error while migrating database {_dbProvider.DbName}", e);
                throw;
            }
           
        }

        /// <summary>
        /// Upgrade database to new version
        /// </summary>
        /// <param name="actualVersion"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        /// <exception cref="MigrationException"></exception>
        private async Task UpgradeAsync(DbVersion actualVersion, DbVersion targetVersion)
        {
            var desiredMigrations = _migrations
                .Where(x => x.Version > actualVersion && x.Version <= targetVersion)
                .OrderBy(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0) return;
            
            var lastMigrationVersion = new DbVersion(0,0);
            var currentDbVersion = actualVersion;
            foreach (var migration in desiredMigrations)
            {
                if (!IsMigrationAllowed(DbVersion.GetDifference(currentDbVersion, migration.Version), _upgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError, $"Policy restrict upgrade to {migration.Version}. Migration comment: {migration.Comment}");
                }
                _logger?.LogInformation($"Upgrading to {migration.Version} (DB {_dbProvider.DbName})...");
                await migration.UpgradeAsync();
                await _dbProvider.UpdateCurrentDbVersionAsync(migration.Version);
                lastMigrationVersion = migration.Version;
                currentDbVersion = migration.Version;
                _logger?.LogInformation($"Upgrading to {migration.Version} (DB {_dbProvider.DbName}) completed.");
            }
            
            if (lastMigrationVersion != targetVersion) throw new MigrationException(
                MigrationError.MigratingError, 
                $"Can not upgrade database to version {targetVersion}. Last executed migration is {lastMigrationVersion}");
        }
   
        /// <summary>
        /// Check permission to migrate using specified policy
        /// </summary>
        /// <param name="versionDifference"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        private bool IsMigrationAllowed(DbVersionDifference versionDifference, MigrationPolicy policy)
        {
            switch (versionDifference)
            {
                case DbVersionDifference.Major:
                    return policy.HasFlag(MigrationPolicy.Major);

                case DbVersionDifference.Minor:
                    return policy.HasFlag(MigrationPolicy.Minor);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Downgrade database to specific version
        /// </summary>
        /// <param name="actualVersion"></param>
        /// <param name="targetVersion"></param>
        /// <returns></returns>
        /// <exception cref="MigrationException"></exception>
        private async Task DowngradeAsync(DbVersion actualVersion, DbVersion targetVersion)
        {
            var desiredMigrations = _migrations
                .Where(x => x.Version < actualVersion && x.Version >= targetVersion)
                .OrderByDescending(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0) return;
            
            var lastMigrationVersion = new DbVersion(0,0);
            var currentDbVersion = actualVersion;
            foreach (var migration in desiredMigrations)
            {
                if (!IsMigrationAllowed(DbVersion.GetDifference(currentDbVersion, migration.Version), _downgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError, $"Policy restrict downgrade to {migration.Version}. Migration comment: {migration.Comment}");
                }
                _logger?.LogInformation($"Downgrade to {migration.Version} (DB {_dbProvider.DbName})...");
                await migration.DowngradeAsync();
                await _dbProvider.UpdateCurrentDbVersionAsync(migration.Version);
                lastMigrationVersion = migration.Version;
                currentDbVersion = migration.Version;
                _logger?.LogInformation($"Downgrade to {migration.Version} (DB {_dbProvider.DbName}) completed.");
            }
            
            if (lastMigrationVersion != targetVersion) throw new MigrationException(
                MigrationError.MigratingError, 
                $"Can not downgrade database to version {targetVersion}. Last executed migration is {lastMigrationVersion}");
        }

        public void Dispose()
        {
            _dbProvider?.Dispose();
        }
    }
}