using System;
using System.Collections.Generic;
using System.Data.Common;
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
        private readonly ILogger? _logger;
        private readonly IDbProvider _dbProvider;
        
        private readonly MigrationPolicy _upgradePolicy;
        private readonly MigrationPolicy _downgradePolicy;
        private readonly DbVersion? _targetVersion;

        private readonly IReadOnlyDictionary<DbVersion, IMigration> _migrationMap;
        private readonly ICollection<IMigration> _preMigrations;

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
            ICollection<IMigration>? preMigrations = null,
            DbVersion? targetVersion = null,
            ILogger? logger = null)
        {
            if (migrations == null) throw new ArgumentNullException(nameof(migrations));
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            
            var migrationMap = new Dictionary<DbVersion, IMigration>();
            foreach (var migration in migrations)
            {
                if (migrationMap.ContainsKey(migration.Version))
                    throw new ArgumentException(
                        $"There is more than one migration with version {migration.Version}", nameof(migrations));

                migrationMap.Add(migration.Version, migration);
            }

            _migrationMap = migrationMap;

            _upgradePolicy = upgradePolicy;
            _downgradePolicy = downgradePolicy;
            _targetVersion = targetVersion;
            _logger = logger;
            
            _preMigrations = preMigrations ?? Array.Empty<IMigration>();
            var preMigrationCheckMap = new HashSet<DbVersion>();
            foreach (var migration in _preMigrations)
            {
                if (preMigrationCheckMap.Contains(migration.Version))
                    throw new ArgumentException($"There is more than one pre-migration with version = {migration.Version}", nameof(preMigrations));

                preMigrationCheckMap.Add(migration.Version);
            }

            if (targetVersion.HasValue)
            {
                var migrationsMaxVersion = _migrationMap.Values.Max(x => x.Version);
                if (targetVersion > migrationsMaxVersion)
                    throw new ArgumentException("Target version can't be greater than max available migration version", nameof(targetVersion));

                if (_migrationMap.Values.All(x => x.Version != targetVersion))
                    throw new ArgumentException($"No migrations were registered with desired target version (target version = {targetVersion})", nameof(targetVersion));
            }
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
                if (_migrationMap.Count == 0)
                {
                    _logger?.LogWarning("No migrations were added to migrator. Skip migration");
                    return;
                }
                
                // check if database exists and create if not
                _logger?.LogInformation($"Check Database \"{_dbProvider.DbName}\" existence...");
                var isDatabaseExist = await _dbProvider.CheckIfDatabaseExistsAsync(_dbProvider.DbName, token);
                if (isDatabaseExist)
                {
                    _logger?.LogInformation($"Database \"{_dbProvider.DbName}\" exists. Starting migration...");
                }
                else
                {
                    _logger?.LogInformation($"Database \"{_dbProvider.DbName}\" doesn't exist. Creating database...");
                    await _dbProvider.CreateDatabaseIfNotExistsAsync(token);
                    _logger?.LogInformation("Creating database completed.");
                }
                
                await _dbProvider.OpenConnectionAsync(token);
                
                // check if applied migrations table exists and create if not 
                _logger?.LogInformation($"Check \"{_dbProvider.AppliedMigrationsTableName}\" table existence...");
                var isMigrationTableExists = await _dbProvider.CheckIfTableExistsAsync(_dbProvider.AppliedMigrationsTableName, token);
                if (isMigrationTableExists)
                {
                    _logger?.LogInformation($"Table \"{_dbProvider.AppliedMigrationsTableName}\" exists. ");
                }
                else
                {
                    _logger?.LogInformation($"Creating \"{_dbProvider.AppliedMigrationsTableName}\" table...");
                    await _dbProvider.CreateHistoryTableIfNotExistsAsync(token);
                    _logger?.LogInformation($"Creating \"{_dbProvider.AppliedMigrationsTableName}\" table completed.");
                }

                // get applied migrations versions
                var (isUpgrade, migrationsToApply) = await GetMigrationsAsync(false, token);

                if (migrationsToApply.Count == 0)
                {
                    _logger?.LogInformation($"Database \"{_dbProvider.DbName}\" is actual. Skip migration.");
                    return;
                }
                
                _logger?.LogInformation($"Executing pre-migration scripts for database \"{_dbProvider.DbName}\"...");
                var wasPreMigrationExecuted = await ExecutePreMigrationScriptsAsync(token);
                if (wasPreMigrationExecuted)
                {
                    _logger?.LogInformation($"Executing pre-migration scripts for database \"{_dbProvider.DbName}\" completed.");

                    // applied migration versions might be changed after pre-migration
                    (isUpgrade, migrationsToApply) = await GetMigrationsAsync(true, token);
                }
                else
                {
                    _logger?.LogInformation("No pre-migration scripts were found.");
                }
                
                if (migrationsToApply.Count == 0)
                {
                    _logger?.LogInformation($"Database \"{_dbProvider.DbName}\" is actual. Skip migration.");
                    return;
                }
                
                _logger?.LogInformation($"Migrating database \"{_dbProvider.DbName}\"...");
                if (isUpgrade)
                {
                    _logger?.LogInformation(
                        $"Upgrading database {_dbProvider.DbName} from {migrationsToApply} to {targetVersion}...");
                    await UpgradeAsync(migrationsToApply, targetVersion, token);
                    _logger?.LogInformation($"Upgrading database {_dbProvider.DbName} completed.");
                }
                else
                {
                    _logger?.LogInformation(
                        $"Downgrading database {_dbProvider.DbName} from {migrationsToApply} to {targetVersion}...");
                    await DowngradeAsync(migrationsToApply, targetVersion, token);
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

        private async Task<(bool isUpgrade, IReadOnlyCollection<IMigration> migrations)> GetMigrationsAsync(bool isPreMigration, CancellationToken token)
        {
            var stageName = isPreMigration ? " after pre-migration" : "";
            _logger?.LogInformation($"Getting migrations to apply{stageName}...");
            
            // build target version
            var maxAvailableMigrationVersion = _migrationMap.Values.Max(x => x.Version);
            var targetVersion = _targetVersion ?? maxAvailableMigrationVersion; // by default we do upgrade to max available version
            
            // get applied migrations versions
            
            var appliedMigrationVersions = await _dbProvider.GetAppliedMigrationVersionAsync(token);

            // provider should order migrations by version ascending, but we can't trust them
            appliedMigrationVersions = appliedMigrationVersions.OrderBy(x => x).ToArray();
            
            var maxAppliedMigration = appliedMigrationVersions.LastOrDefault();
            
            if (appliedMigrationVersions.Count == 0)
            {
                _logger?.LogInformation("No migrations were applied.");
            }
            else
            {
                _logger?.LogInformation($"Max applied migration version = {maxAppliedMigration}. " +
                                        $"Applied migrations count = {appliedMigrationVersions.Count}");
            }
            
            // build list of migrations to apply
            
            var availableMigrationVersions = new HashSet<DbVersion>(_migrationMap.Values.Select(x => x.Version));
            
            var migrationsToApply = new List<IMigration>();

            bool isUpgrade;
            // it's upgrade
            if (targetVersion >= maxAppliedMigration) // no strictly comparision because of patch migration strategy
            {
                // we need only not applied migrations
                availableMigrationVersions.ExceptWith(appliedMigrationVersions);
                var notAppliedMigrationVersions = availableMigrationVersions
                    .Where(x => x <= targetVersion)
                    .OrderBy(x => x);

                foreach (var notAppliedMigrationVersion in notAppliedMigrationVersions)
                {
                    var notAppliedMigration = _migrationMap[notAppliedMigrationVersion];
                    migrationsToApply.Add(notAppliedMigration);
                }

                isUpgrade = true;
            }
            else // it's downgrade
            {
                // we need only applied migrations
                // we do not check implementation of IDowngradableMigration here
                availableMigrationVersions.IntersectWith(appliedMigrationVersions);
                var migrationsVersionsToApply = availableMigrationVersions
                    .Where(x => x > targetVersion)
                    .OrderByDescending(x => x);

                foreach (var downgradeMigrationVersion in migrationsVersionsToApply)
                {
                    var downgradeMigration = _migrationMap[downgradeMigrationVersion];
                    migrationsToApply.Add(downgradeMigration);
                }

                isUpgrade = false;
            }

            _logger?.LogInformation($"Getting migrations to apply{stageName} completed.");
            
            return (isUpgrade, migrationsToApply);
        }

        /// <summary>
        /// Executes pre-migration scripts.
        /// </summary>
        /// <returns>True if pre-migrations were executed, otherwise - false.</returns>
        /// <exception cref="MigrationException"></exception>
        private async Task<bool> ExecutePreMigrationScriptsAsync(CancellationToken token = default)
        {
            var desiredMigrations = _preMigrations
                .OrderBy(x => x.Version)
                .ToArray();
            if (desiredMigrations.Length == 0)
                return false;

            foreach (var migration in desiredMigrations)
            {
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync(token);
                using (var transaction = _dbProvider.BeginTransaction())
                {
                    try
                    {
                        _logger?.LogInformation(
                            $"Executing pre-migration script {migration.Version} (\"{migration.Comment}\") for DB \"{_dbProvider.DbName}\"...");
                        await migration.UpgradeAsync(transaction, token);

                        transaction.Commit();

                        _logger?.LogInformation(
                            $"Executing pre-migration script {migration.Version} for DB \"{_dbProvider.DbName}\" completed.");
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e,
                            $"Error while executing pre-migration to {migration.Version} for DB \"{_dbProvider.DbName}\": {e.Message}");
                        throw;
                    }
                }
            }
            return true;
        }

        private async Task MigrateAsync(IReadOnlyCollection<IMigration> orderedMigrations, bool isUpgrade, MigrationPolicy policy, CancellationToken token = default)
        {
            if (policy == MigrationPolicy.Forbidden)
                throw new MigrationException(MigrationError.PolicyError, $"{(isUpgrade ? "Upgrading": "Downgrading")} is forbidden due to migration policy");
            
            if (orderedMigrations.Count == 0) return;
            
            var operationName = isUpgrade
                ? "Upgrade"
                : "Downgrade;";
            foreach (var migration in desiredMigrations)
            {
                token.ThrowIfCancellationRequested();
                
                if (!IsMigrationAllowed(DbVersion.GetDifference(currentDbVersion, migration.Version), _upgradePolicy))
                {
                    throw new MigrationException(MigrationError.PolicyError,
                        $"Policy restrict upgrade to {migration.Version}. Migration comment: {migration.Comment}");
                }

                DbTransaction? transaction = null;

                try
                {
                    // sometimes transactions fails without reopening connection
                    //todo fix it later
                    await _dbProvider.CloseConnectionAsync();
                    await _dbProvider.OpenConnectionAsync(token);

                    if (migration.IsTransactionRequired)
                    {
                        transaction = _dbProvider.BeginTransaction();
                    }
                    else
                    {
                        _logger?.LogWarning($"Transaction is disabled for migration \"{migration.Version}\"");
                    }
                    
                    _logger?.LogInformation($"Upgrade to {migration.Version} ({migration.Comment} for DB {_dbProvider.DbName})...");
                    await migration.UpgradeAsync(transaction, token);
                    await _dbProvider.SaveAppliedMigrationVersionAsync(migration.Comment, migration.Version, token);
                    lastMigrationVersion = migration.Version;
                    currentDbVersion = migration.Version;

                    // Commit transaction if all commands succeed, transaction will auto-rollback
                    // when disposed if either commands fails
                    transaction?.Commit();

                    _logger?.LogInformation($"Upgrade to {migration.Version} (DB {_dbProvider.DbName}) completed.");
                }
                finally
                {
                    transaction?.Dispose();
                }
            }

            if (lastMigrationVersion != targetVersion)
                throw new MigrationException(
                    MigrationError.MigratingError,
                    $"Can not upgrade database to version {targetVersion}. Last executed migration is {lastMigrationVersion}");
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
            
            var desiredMigrations = _migrationMap
                .Where(x => x.Version > actualVersion && x.Version <= targetVersion)
                .OrderBy(x => x.Version)
                .ToArray();
            if (desiredMigrations.Length == 0) return;

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

                DbTransaction? transaction = null;

                try
                {
                    // sometimes transactions fails without reopening connection
                    //todo fix it later
                    await _dbProvider.CloseConnectionAsync();
                    await _dbProvider.OpenConnectionAsync(token);

                    if (migration.IsTransactionRequired)
                    {
                        transaction = _dbProvider.BeginTransaction();
                    }
                    else
                    {
                        _logger?.LogWarning($"Transaction is disabled for migration \"{migration.Version}\"");
                    }
                    
                    _logger?.LogInformation($"Upgrade to {migration.Version} ({migration.Comment} for DB {_dbProvider.DbName})...");
                    await migration.UpgradeAsync(transaction, token);
                    await _dbProvider.SaveAppliedMigrationVersionAsync(migration.Comment, migration.Version, token);
                    lastMigrationVersion = migration.Version;
                    currentDbVersion = migration.Version;

                    // Commit transaction if all commands succeed, transaction will auto-rollback
                    // when disposed if either commands fails
                    transaction?.Commit();

                    _logger?.LogInformation($"Upgrade to {migration.Version} (DB {_dbProvider.DbName}) completed.");
                }
                finally
                {
                    transaction?.Dispose();
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
            
            var desiredMigrations = _migrationMap
                .Where(x => x.Version <= actualVersion && x.Version >= targetVersion)
                .OrderByDescending(x => x.Version)
                .ToArray();
            if (desiredMigrations.Length == 0 || desiredMigrations.Length == 1) return;

            var downgradableMigrationsCount = _migrationMap.Count(x => x is IDowngradeMigration);
            if (downgradableMigrationsCount != desiredMigrations.Length)
                throw new MigrationException(MigrationError.MigrationNotFound, $"Found {downgradableMigrationsCount} downgrade migrations but expected {desiredMigrations.Length}");
            
            var lastMigrationVersion = new DbVersion(0, 0);
            var currentDbVersion = actualVersion;
            for (var i = 0; i < desiredMigrations.Length - 1; ++i)
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
                    _logger?.LogInformation(
                        $"Downgrade to {desiredMigrations[i + 1].Version} (DB {_dbProvider.DbName})...");
                    await downgradeMigration.DowngradeAsync(transaction, token);
                    await _dbProvider.SaveAppliedMigrationVersionAsync(downgradeMigration.Comment, targetLocalVersion, token);
                    lastMigrationVersion = targetLocalVersion;
                    currentDbVersion = targetLocalVersion;

                    // Commit transaction if all commands succeed, transaction will auto-rollback
                    // when disposed if either commands fails
                    transaction.Commit();

                    _logger?.LogInformation($"Downgrade to {targetLocalVersion} (DB {_dbProvider.DbName}) completed.");
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
            _dbProvider.Dispose();
        }
    }
}