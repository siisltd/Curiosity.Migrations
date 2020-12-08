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
            _logger = logger;

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
            
            _preMigrations = preMigrations ?? Array.Empty<IMigration>();
            var preMigrationCheckMap = new HashSet<DbVersion>();
            foreach (var migration in _preMigrations)
            {
                if (preMigrationCheckMap.Contains(migration.Version))
                    throw new ArgumentException($"There is more than one pre-migration with version = {migration.Version}", nameof(preMigrations));

                preMigrationCheckMap.Add(migration.Version);
            }

            if (targetVersion.HasValue && migrationMap.Values.Count > 0)
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
                _logger?.LogError(e, e.Message);
                return MigrationResult.FailureResult(e.Error, e.Message);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"Error while migrating database {_dbProvider.DbName}. Reason: {e.Message}");
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
                    await _dbProvider.CreateAppliedMigrationsTableIfNotExistsAsync(token);
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
                var policy = isUpgrade
                    ? _upgradePolicy
                    : _downgradePolicy;
                await MigrateAsync(migrationsToApply, isUpgrade, policy, token);

                await _dbProvider.CloseConnectionAsync();
                _logger?.LogInformation($"Migrating database {_dbProvider.DbName} completed.");
            }
            catch (MigrationException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new MigrationException(
                    MigrationError.MigratingError,
                    $"Error while migrating database {_dbProvider.DbName}. Reason: {e.Message}",
                    e);
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
                token.ThrowIfCancellationRequested();
                
                DbTransaction? transaction = null;
                
                await _dbProvider.CloseConnectionAsync();
                await _dbProvider.OpenConnectionAsync(token);

                try
                {
                    _logger?.LogInformation(
                        $"Executing pre-migration script {migration.Version} (\"{migration.Comment}\") for database \"{_dbProvider.DbName}\"...");
                    
                    if (migration.IsTransactionRequired)
                    {
                        transaction = _dbProvider.BeginTransaction();
                    }
                    else
                    {
                        _logger?.LogWarning($"Transaction is disabled for pre-migration {migration.Version}");
                    }
                    
                    await migration.UpgradeAsync(transaction, token);

                    transaction?.Commit();

                    _logger?.LogInformation(
                        $"Executing pre-migration script {migration.Version} for database \"{_dbProvider.DbName}\" completed.");
                }
                catch (Exception e)
                {
                    throw new MigrationException(
                        MigrationError.MigratingError,
                        $"Error while executing pre-migration to {migration.Version} for database \"{_dbProvider.DbName}\": {e.Message}",
                        e);
                }
                finally
                {
                    transaction?.Dispose();
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
                : "Downgrade";
            foreach (var migration in orderedMigrations)
            {
                token.ThrowIfCancellationRequested();
                
                DbTransaction? transaction = null;

                try
                {
                    // sometimes transactions fails without reopening connection
                    //todo #19: fix it later
                    await _dbProvider.CloseConnectionAsync();
                    await _dbProvider.OpenConnectionAsync(token);

                    if (migration.IsTransactionRequired)
                    {
                        transaction = _dbProvider.BeginTransaction();
                    }
                    else
                    {
                        _logger?.LogWarning($"Transaction is disabled for migration {migration.Version}");
                    }
                    
                    _logger?.LogInformation($"{operationName} to {migration.Version} ({migration.Comment} for database \"{_dbProvider.DbName}\")...");
                    
                    if (isUpgrade)
                    {
                        await migration.UpgradeAsync(transaction, token);
                        await _dbProvider.SaveAppliedMigrationVersionAsync(migration.Comment, migration.Version, token);
                    }
                    else
                    {
                        if (!(migration is IDowngradeMigration downgradableMigration))
                            throw new MigrationException(MigrationError.MigrationNotFound, $"Migration with version {migration.Version} doesn't support downgrade");

                        await downgradableMigration.DowngradeAsync(transaction, token);
                        await _dbProvider.DeleteAppliedMigrationVersionAsync(migration.Version, token);
                    }

                    // Commit transaction if all commands succeed, transaction will auto-rollback
                    // when disposed if either commands fails
                    transaction?.Commit();

                    _logger?.LogInformation($"{operationName} to {migration.Version} (database \"{_dbProvider.DbName}\") completed.");
                }
                catch (Exception e)
                {
                    throw new MigrationException(
                        MigrationError.MigratingError,
                        $"Error while executing {operationName.ToLower()} migration to {migration.Version} for database \"{_dbProvider.DbName}\": {e.Message}",
                        e);
                }
                finally
                {
                    transaction?.Dispose();
                }
            }
        }
        
        /// <inheritdoc />
        public void Dispose()
        {
            _dbProvider.Dispose();
        }
    }
}