using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        public async Task<MigrationResult> MigrateSafeAsync(CancellationToken token = default)
        {
            try
            {
                await MigrateAsync(token);
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
        public async Task MigrateAsync(CancellationToken token = default)
        {
            try
            {
                _logger?.LogInformation($"Check {_dbProvider.DbName} existence...");
                var isDatabaseExist = await _dbProvider.CheckIfDatabaseExistsAsync(_dbProvider.DbName, token);
                if (isDatabaseExist)
                {
                    _logger?.LogInformation($"{_dbProvider.DbName} exists. Starting migration...");
                }
                else
                {
                    _logger?.LogInformation($"{_dbProvider.DbName} doesn't exist. Creating database...");
                    await _dbProvider.CreateDatabaseIfNotExistsAsync(token);
                    _logger?.LogInformation("Creating database completed.");
                }
                
                await _dbProvider.OpenConnectionAsync(token);
                
                _logger?.LogInformation($"Check {_dbProvider.MigrationHistoryTableName} table existence...");
                var isMigrationTableExists = await _dbProvider.CheckIfTableExistsAsync(_dbProvider.MigrationHistoryTableName, token);
                if (isMigrationTableExists)
                {
                    _logger?.LogInformation($"{_dbProvider.MigrationHistoryTableName} exists. ");
                }
                else
                {
                    _logger?.LogInformation($"Creating {_dbProvider.MigrationHistoryTableName} table...");
                    await _dbProvider.CreateHistoryTableIfNotExistsAsync(token);
                    _logger?.LogInformation($"Creating {_dbProvider.MigrationHistoryTableName} table completed.");
                }

                var isDowngradeEnabled = _downgradePolicy != MigrationPolicy.Forbidden;
                
                _logger?.LogInformation("Check database version...");
                var dbVersion = await _dbProvider.GetDbVersionSafeAsync(isDowngradeEnabled, token) ?? default;

                var targetVersion = _targetVersion ?? _migrations.Max(x => x.Version);
                if (targetVersion == dbVersion)
                {
                    _logger?.LogInformation($"Database {_dbProvider.DbName} is actual. Skip migration.");
                    return;
                }

                _logger?.LogInformation(dbVersion == default
                    ? "No entries at history table were found."
                    : $"Current database version is {dbVersion}.");

                if (_migrations.All(x => x.Version != targetVersion))
                    throw new MigrationException(MigrationError.MigrationNotFound,
                        $"Migration {targetVersion} not found");
                
                _logger?.LogInformation($"Executing pre migration scripts for {_dbProvider.DbName}...");
                var wasPreMigrationExecuted = await ExecutePreMigrationScriptsAsync(token);
                if (wasPreMigrationExecuted)
                {
                    _logger?.LogInformation($"Executing pre migration scripts for {_dbProvider.DbName} completed.");

                    // DB version might be changed after pre-migration
                    _logger?.LogInformation("Check database version after pre migration...");
                    dbVersion = await _dbProvider.GetDbVersionSafeAsync(isDowngradeEnabled, token) ?? default;   
                    _logger?.LogInformation("Check database version after pre migration...");
                }
                else
                {
                    _logger?.LogInformation("No pre migration scripts were executed.");
                }

                _logger?.LogInformation($"Migrating database {_dbProvider.DbName}...");
                if (targetVersion > dbVersion)
                {
                    _logger?.LogInformation(
                        $"Upgrading database {_dbProvider.DbName} from {dbVersion} to {targetVersion}...");
                    await UpgradeAsync(dbVersion, targetVersion, token);
                    _logger?.LogInformation($"Upgrading database {_dbProvider.DbName} completed.");
                }
                else
                {
                    _logger?.LogInformation(
                        $"Downgrading database {_dbProvider.DbName} from {dbVersion} to {targetVersion}...");
                    await DowngradeAsync(dbVersion, targetVersion, token);
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

        /// <summary>
        /// Upgrade database to new version
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MigrationException"></exception>
        private async Task<bool> ExecutePreMigrationScriptsAsync(CancellationToken token = default)
        {
            var desiredMigrations = _preMigrations
                .OrderBy(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0)
            {
                return false;
            }

            foreach (var migration in desiredMigrations)
            {
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync(token);
                using (var transaction = _dbProvider.BeginTransaction())
                {
                    try
                    {
                        _logger?.LogInformation(
                            $"Executing pre migration script {migration.Version} ({migration.Comment}) for DB {_dbProvider.DbName}...");
                        await migration.UpgradeAsync(transaction, token);

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
            return true;
        }

        /// <summary>
        /// Upgrade database to new version
        /// </summary>
        /// <param name="actualVersion"></param>
        /// <param name="targetVersion"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="MigrationException"></exception>
        private async Task UpgradeAsync(DbVersion actualVersion, DbVersion targetVersion, CancellationToken token = default)
        {
            if (_upgradePolicy == MigrationPolicy.Forbidden)
                throw new MigrationException(MigrationError.PolicyError, "Upgrading is forbidden due to migration upgrade policy");
            
            var desiredMigrations = _migrations
                .Where(x => x.Version > actualVersion && x.Version <= targetVersion)
                .OrderBy(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0) return;

            var lastMigrationVersion = new DbVersion(0, 0);
            var currentDbVersion = actualVersion;
            foreach (var migration in desiredMigrations)
            {
                token.ThrowIfCancellationRequested();
                
                if (!IsMigrationAllowed(DbVersion.GetDifference(currentDbVersion, migration.Version), _upgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError,
                        $"Policy restrict upgrade to {migration.Version}. Migration comment: {migration.Comment}");
                }

                // sometimes transactions fails without reopening connection
                //todo fix it later
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync(token);

                using (var transaction = _dbProvider.BeginTransaction())
                {
                    try
                    {
                        _logger?.LogInformation($"Upgrade to {migration.Version} ({migration.Comment} for DB {_dbProvider.DbName})...");
                        await migration.UpgradeAsync(transaction, token);
                        await _dbProvider.UpdateCurrentDbVersionAsync(migration.Comment, migration.Version, token);
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
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="MigrationException"></exception>
        private async Task DowngradeAsync(DbVersion actualVersion, DbVersion targetVersion, CancellationToken token = default)
        {
            if (_downgradePolicy == MigrationPolicy.Forbidden)
                throw new MigrationException(MigrationError.PolicyError, "Downgrading is forbidden due to migration downrgade policy");
            
            var desiredMigrations = _migrations
                .Where(x => x.Version <= actualVersion && x.Version >= targetVersion)
                .OrderByDescending(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0 || desiredMigrations.Count == 1) return;

            var downgradableMigrationsCount = _migrations.Count(x => x is IDowngradeMigration);
            if (downgradableMigrationsCount != desiredMigrations.Count)
                throw new MigrationException(MigrationError.MigrationNotFound, $"Found {downgradableMigrationsCount} downgrade migrations but expected {desiredMigrations.Count}");
            
            var lastMigrationVersion = new DbVersion(0, 0);
            var currentDbVersion = actualVersion;
            for (var i = 0; i < desiredMigrations.Count - 1; ++i)
            {
                token.ThrowIfCancellationRequested();
                
                var targetLocalVersion = desiredMigrations[i + 1].Version;
                var migration = desiredMigrations[i];
                
                if (!(migration is IDowngradeMigration downgradeMigration))
                    throw new MigrationException(MigrationError.MigrationNotFound, $"Migration {migration.Version} doesn't support downgrade");

                if (!IsMigrationAllowed(DbVersion.GetDifference(currentDbVersion, targetLocalVersion),
                    _downgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError,
                        $"Policy restrict downgrade to {targetLocalVersion}. Migration comment: {migration.Comment}");
                }

                // sometimes transactions fails without reopening connection
                //todo fix it later
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync(token);

                using (var transaction = _dbProvider.BeginTransaction())
                {
                    try
                    {
                        _logger?.LogInformation(
                            $"Downgrade to {desiredMigrations[i + 1].Version} (DB {_dbProvider.DbName})...");
                        await downgradeMigration.DowngradeAsync(transaction, token);
                        await _dbProvider.UpdateCurrentDbVersionAsync(downgradeMigration.Comment, targetLocalVersion, token);
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