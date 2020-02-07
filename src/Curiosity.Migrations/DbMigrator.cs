using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Default realisation of <see cref="IDbMigrator"/>
    /// </summary>
    public sealed class DbMigrator : IDbMigrator, IDisposable
    {
        private readonly MigrationPolicy _upgradePolicy;
        private readonly MigrationPolicy _downgradePolicy;

        private readonly ICollection<IMigration> _preMigrations;
        private readonly ICollection<IMigration> _migrations;

        private readonly ILogger _logger;
        private readonly IDbProvider _dbProvider;
        private readonly DbVersion? _targetVersion;

        /// <summary>
        /// Default realisation of <see cref="IDbMigrator"/>
        /// </summary>
        /// <param name="dbProvider">Provider for database</param>
        /// <param name="migrations">Main migrations for changing database</param>
        /// <param name="upgradePolicy">Policy for upgrading database</param>
        /// <param name="downgradePolicy">Policy for downgrading database</param>
        /// <param name="preMigrations">Migrations that will be executed before <paramref name="migrations"/></param>
        /// <param name="targetVersion">Desired version of database after migration. If <see langword="null"/> migrator will upgrade database to the most actual version</param>
        /// <param name="logger">Optional logger</param>
        public DbMigrator(
            IDbProvider dbProvider,
            ICollection<IMigration> migrations,
            MigrationPolicy upgradePolicy,
            MigrationPolicy downgradePolicy,
            ICollection<IMigration> preMigrations = null,
            DbVersion? targetVersion = null,
            ILogger logger = null)
        {
            if (migrations == null) throw new ArgumentNullException(nameof(migrations));
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));

            _migrations = migrations.ToList();

            var migrationCheckMap = new HashSet<DbVersion>();
            foreach (var migration in _migrations)
            {
                if (migrationCheckMap.Contains(migration.Version))
                    throw new InvalidOperationException(
                        $"There is more than one migration with version {migration.Version}");

                migrationCheckMap.Add(migration.Version);
            }

            _upgradePolicy = upgradePolicy;
            _downgradePolicy = downgradePolicy;
            _preMigrations = preMigrations ?? new List<IMigration>(0);
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
                await _dbProvider.CreateDatabaseIfNotExistsAsync();
                await _dbProvider.OpenConnectionAsync();
                var dbVersion = await GetCurrentDbVersion();

                var targetVersion = _targetVersion ?? _migrations.Max(x => x.Version);
                if (targetVersion == dbVersion)
                {
                    _logger?.LogInformation($"Database {_dbProvider.DbName} is actual. Skip migration.");
                    return;
                }

                if (_migrations.All(x => x.Version != targetVersion))
                    throw new MigrationException(MigrationError.MigrationNotFound,
                        $"Migration {targetVersion} not found");


                _logger?.LogInformation($"Executing pre migration scripts for {_dbProvider.DbName}...");
                await ExecutePreMigrationScriptsAsync();
                _logger?.LogInformation($"Executing pre migration scripts for{_dbProvider.DbName} completed.");

                await _dbProvider.CreateHistoryTableIfNotExistsAsync();

                // DB version might change after pre-migration
                dbVersion = await GetCurrentDbVersion();

                _logger?.LogInformation($"Migrating database {_dbProvider.DbName}...");
                if (targetVersion > dbVersion)
                {
                    _logger?.LogInformation(
                        $"Upgrading database {_dbProvider.DbName} from {dbVersion} to {targetVersion}...");
                    await UpgradeAsync(dbVersion, targetVersion);
                    _logger?.LogInformation($"Upgrading database {_dbProvider.DbName} completed.");
                }
                else
                {
                    _logger?.LogInformation(
                        $"Downgrading database {_dbProvider.DbName} from {dbVersion} to {targetVersion}...");
                    await DowngradeAsync(dbVersion, targetVersion);
                    _logger?.LogInformation($"Downgrading database {_dbProvider.DbName} completed.");
                }

                await _dbProvider.CloseConnectionAsync();
                _logger?.LogInformation($"Migrating database {_dbProvider.DbName} completed.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error while migrating database {_dbProvider.DbName}");
                throw;
            }
        }

        private async Task<DbVersion> GetCurrentDbVersion()
        {
            return await _dbProvider.GetDbVersionSafeAsync() 
                   ?? new DbVersion(0, 0);
        }

        /// <summary>
        /// Upgrade database to new version
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MigrationException"></exception>
        private async Task ExecutePreMigrationScriptsAsync()
        {
            var desiredMigrations = _preMigrations
                .OrderBy(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0) return;

            foreach (var migration in desiredMigrations)
            {
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync();
                using (var transaction = _dbProvider.BeginTransaction())
                {
                    try
                    {
                        _logger?.LogInformation(
                            $"Executing pre migration script {migration.Version} ({migration.Comment}) for DB {_dbProvider.DbName}...");
                        await migration.UpgradeAsync(transaction);

                        transaction.Commit();

                        _logger?.LogInformation(
                            $"Executing pre migration script {migration.Version} for DB {_dbProvider.DbName}) completed.");
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e,
                            $"Error while executing pre migration to {migration.Version}: {e.Message}");
                        throw;
                    }
                }
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

            var lastMigrationVersion = new DbVersion(0, 0);
            var currentDbVersion = actualVersion;
            foreach (var migration in desiredMigrations)
            {
                if (!IsMigrationAllowed(DbVersion.GetDifference(currentDbVersion, migration.Version), _upgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError,
                        $"Policy restrict upgrade to {migration.Version}. Migration comment: {migration.Comment}");
                }

                // sometimes transactions fails without reopening connection
                //todo fix it later
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync();

                using (var transaction = _dbProvider.BeginTransaction())
                {
                    try
                    {
                        _logger?.LogInformation($"Upgrade to {migration.Version} (DB {_dbProvider.DbName})...");
                        await migration.UpgradeAsync(transaction);
                        await _dbProvider.UpdateCurrentDbVersionAsync(migration.Version);
                        lastMigrationVersion = migration.Version;
                        currentDbVersion = migration.Version;

                        // Commit transaction if all commands succeed, transaction will auto-rollback
                        // when disposed if either commands fails
                        transaction.Commit();

                        _logger?.LogInformation($"Upgrade to {migration.Version} (DB {_dbProvider.DbName}) completed.");
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, $"Error while upgrade to {migration.Version}: {e.Message}");
                        throw;
                    }
                }
            }

            if (lastMigrationVersion != targetVersion)
                throw new MigrationException(
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
                .Where(x => x.Version <= actualVersion && x.Version >= targetVersion)
                .OrderByDescending(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0 || desiredMigrations.Count == 1) return;

            var lastMigrationVersion = new DbVersion(0, 0);
            var currentDbVersion = actualVersion;
            for (var i = 0; i < desiredMigrations.Count - 1; ++i)
            {
                var targetLocalVersion = desiredMigrations[i + 1].Version;
                var migration = desiredMigrations[i];

                if (!IsMigrationAllowed(DbVersion.GetDifference(currentDbVersion, targetLocalVersion),
                    _downgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError,
                        $"Policy restrict downgrade to {targetLocalVersion}. Migration comment: {migration.Comment}");
                }

                // sometimes transactions fails without reopening connection
                //todo fix it later
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync();

                using (var transaction = _dbProvider.BeginTransaction())
                {
                    try
                    {
                        _logger?.LogInformation(
                            $"Downgrade to {desiredMigrations[i + 1].Version} (DB {_dbProvider.DbName})...");
                        await desiredMigrations[i].DowngradeAsync(transaction);
                        await _dbProvider.UpdateCurrentDbVersionAsync(targetLocalVersion);
                        lastMigrationVersion = targetLocalVersion;
                        currentDbVersion = targetLocalVersion;

                        // Commit transaction if all commands succeed, transaction will auto-rollback
                        // when disposed if either commands fails
                        transaction.Commit();

                        _logger?.LogInformation(
                            $"Downgrade to {targetLocalVersion} (DB {_dbProvider.DbName}) completed.");
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, $"Error while downgrade to {migration.Version}: {e.Message}");
                        throw;
                    }
                }
            }

            if (lastMigrationVersion != targetVersion)
                throw new MigrationException(
                    MigrationError.MigratingError,
                    $"Can not downgrade database to version {targetVersion}. Last executed migration is {lastMigrationVersion}");
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _dbProvider?.Dispose();
        }
    }
}