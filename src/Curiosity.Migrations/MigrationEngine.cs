using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations;

/// <summary>
/// Default realisation of <see cref="IMigrationEngine"/>
/// </summary>
public sealed class MigrationEngine : IMigrationEngine, IDisposable
{
    private readonly ILogger? _logger;
    private readonly IMigrationConnection _migrationConnection;

    private readonly MigrationPolicy _upgradePolicy;
    private readonly MigrationPolicy _downgradePolicy;
    private readonly MigrationVersion? _targetVersion;
    private readonly bool _onlyTargetVersion;

    private readonly IReadOnlyDictionary<MigrationVersion, IMigration> _availableMigrationsMap;
    private readonly IReadOnlyList<IMigration> _availablePreMigrations;

    /// <inheritdoc cref="MigrationEngine"/>
    /// <param name="migrationConnection">Provider for database</param>
    /// <param name="migrations">Main migrations for changing database</param>
    /// <param name="upgradePolicy">Policy for upgrading database</param>
    /// <param name="downgradePolicy">Policy for downgrading database</param>
    /// <param name="preMigrations">Migrations that will be executed before <paramref name="migrations"/></param>
    /// <param name="targetVersion">
    /// Target migration version. Options for upgrades. Required for downgrades.
    /// If specified, migrator will upgrade or downgrade database depending on the current applied version and the specified.
    /// If not specified, migrator will apply all migrations, provided by registered instances of <see cref="IMigrationsProvider"/> for upgrade,
    /// and already applied migrations for a downgrade if they provided by registered instances of <see cref="IMigrationsProvider"/>. 
    /// </param>
    /// <param name="onlyTargetVersion">
    /// Should engine executes only specified target migration?
    /// Otherwise all migration with version less than target will be used for upgrade and all migration with version greater than applied will be used for downgrade.</param>
    /// <param name="logger">Optional logger</param>
    public MigrationEngine(
        IMigrationConnection migrationConnection,
        IReadOnlyList<IMigration> migrations,
        MigrationPolicy upgradePolicy,
        MigrationPolicy downgradePolicy,
        IReadOnlyList<IMigration>? preMigrations = null,
        MigrationVersion? targetVersion = null,
        bool onlyTargetVersion = false,
        ILogger? logger = null)
    {
        Guard.AssertNotNull(migrationConnection, nameof(migrationConnection));
        Guard.AssertNotNull(migrations, nameof(migrations));

        _migrationConnection = migrationConnection;
        _logger = logger;

        var migrationMap = new Dictionary<MigrationVersion, IMigration>();
        var temp = migrations.OrderBy(x => x.Version).ToArray();
        for (var i = 0; i < temp.Length; i++)
        {
            var migration = migrations[i];
            if (migrationMap.ContainsKey(migration.Version))
                throw new ArgumentException(
                    $"There is more than one migration with version {migration.Version}", nameof(migrations));

            migrationMap.Add(migration.Version, migration);
        }

        _availableMigrationsMap = migrationMap;

        _upgradePolicy = upgradePolicy;
        _downgradePolicy = downgradePolicy;
        _targetVersion = targetVersion;
        _onlyTargetVersion = onlyTargetVersion;

        _availablePreMigrations = preMigrations?.OrderBy(x => x.Version).ToArray()
                                  ?? Array.Empty<IMigration>();
        var preMigrationCheckMap = new HashSet<MigrationVersion>();
        for (var i = 0; i < _availablePreMigrations.Count; i++)
        {
            var migration = _availablePreMigrations[i];
            if (preMigrationCheckMap.Contains(migration.Version))
                throw new ArgumentException($"There is more than one pre-migration with version = {migration.Version}", nameof(preMigrations));

            preMigrationCheckMap.Add(migration.Version);
        }

        if (targetVersion.HasValue && migrationMap.Values.Count > 0)
        {
            var migrationsMaxVersion = _availableMigrationsMap.Values.Max(x => x.Version);
            if (targetVersion > migrationsMaxVersion)
                throw new ArgumentException("Target version can't be greater than max available migration version", nameof(targetVersion));

            if (_availableMigrationsMap.Values.All(x => x.Version != targetVersion))
                throw new ArgumentException($"No migrations were registered with desired target version (target version = {targetVersion})", nameof(targetVersion));
        }
    }

    /// <inheritdoc />
    public Task<MigrationResult> UpgradeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        return MigrateAsync(true, cancellationToken);
    }

    /// <inheritdoc />
    public Task<MigrationResult> DowngradeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (_targetVersion == null)
            throw new InvalidOperationException("Can't execute database downgrade because target version wasn't specified. Target version is required for downgrade migrations");

        return MigrateAsync(false, cancellationToken);
    }

    /// <summary>
    /// Executes migration of a database applying all migration according to migrator's configuration.
    /// </summary>
    private async Task<MigrationResult> MigrateAsync(
        bool isUpgrade,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_availableMigrationsMap.Count == 0)
            {
                _logger?.LogWarning("No migrations were added to migrator. Stop migration");
                return MigrationResult.CreateSuccessful(
                    Array.Empty<MigrationInfo>(),
                    Array.Empty<MigrationInfo>());
            }

            // check if database exists and create if not
            _logger?.LogInformation($"Check database \"{_migrationConnection.DatabaseName}\" existence...");
            var isDatabaseExist = await _migrationConnection.CheckIfDatabaseExistsAsync(_migrationConnection.DatabaseName, cancellationToken);
            if (isDatabaseExist)
            {
                _logger?.LogInformation($"Database \"{_migrationConnection.DatabaseName}\" already exists. Starting migration...");
            }
            else
            {
                _logger?.LogInformation($"Database \"{_migrationConnection.DatabaseName}\" doesn't exist. Creating database...");
                await _migrationConnection.CreateDatabaseIfNotExistsAsync(cancellationToken);
                _logger?.LogInformation("Creating database completed");
            }

            await _migrationConnection.OpenConnectionAsync(cancellationToken);

            // check if applied migrations table exists and create if not 
            _logger?.LogInformation($"Check \"{_migrationConnection.MigrationHistoryTableName}\" table existence...");
            var isMigrationTableExists = await _migrationConnection.CheckIfTableExistsAsync(_migrationConnection.MigrationHistoryTableName, cancellationToken);
            if (isMigrationTableExists)
            {
                _logger?.LogInformation($"Table \"{_migrationConnection.MigrationHistoryTableName}\" exists");
            }
            else
            {
                _logger?.LogInformation($"Creating \"{_migrationConnection.MigrationHistoryTableName}\" table...");
                await _migrationConnection.CreateMigrationHistoryTableIfNotExistsAsync(cancellationToken);
                _logger?.LogInformation($"Creating \"{_migrationConnection.MigrationHistoryTableName}\" table completed");
            }

            // get migrations to apply
            var migrationsToApply = await GetMigrationsToApplyAsync(isUpgrade, false, cancellationToken);
            if (migrationsToApply.Count == 0)
            {
                _logger?.LogInformation($"Database \"{_migrationConnection.DatabaseName}\" is actual. Stop migration");
                return MigrationResult.CreateSuccessful(
                    Array.Empty<MigrationInfo>(),
                    Array.Empty<MigrationInfo>());
            }

            _logger?.LogInformation($"Executing pre-migration scripts for database \"{_migrationConnection.DatabaseName}\"...");
            var wasPreMigrationExecuted = await ExecutePreMigrationScriptsAsync(cancellationToken);
            if (wasPreMigrationExecuted)
            {
                _logger?.LogInformation($"Executing pre-migration scripts for database \"{_migrationConnection.DatabaseName}\" completed");

                // applied migration versions might be changed after pre-migration
                migrationsToApply = await GetMigrationsToApplyAsync(isUpgrade, true, cancellationToken);
            }
            else
            {
                _logger?.LogInformation("No pre-migration scripts were found");
            }

            if (migrationsToApply.Count == 0)
            {
                _logger?.LogInformation($"Database \"{_migrationConnection.DatabaseName}\" is actual. Stop migration");
                return MigrationResult.CreateSuccessful(
                    Array.Empty<MigrationInfo>(),
                    Array.Empty<MigrationInfo>());
            }

            _logger?.LogInformation($"Migrating database \"{_migrationConnection.DatabaseName}\"...");
            var policy = isUpgrade
                ? _upgradePolicy
                : _downgradePolicy;
            var migrationResult = await MigrateAsync(migrationsToApply, isUpgrade, policy, cancellationToken);

            await _migrationConnection.CloseConnectionAsync();
            _logger?.LogInformation($"Migrating database \"{_migrationConnection.DatabaseName}\" completed. Successfully applied {migrationsToApply.Count} migrations");

            return MigrationResult.CreateSuccessful(migrationResult.Applied, migrationResult.Skipped);
        }
        catch (MigrationException e)
        {
            _logger?.LogError(
                e,
                e.MigrationInfo != null
                    ? $"Failed to execute migration \"{e.MigrationInfo.Value.Version}\" (ErrorCode={e.ErrorCode}): {e.Message}"
                    : $"Failed to execute migration (ErrorCode={e.ErrorCode}): {e.Message}");

            return MigrationResult.CreateFailed(
                e.ErrorCode,
                e.Message,
                e.MigrationInfo);
        }
        catch (Exception e)
        {
            var errorMessage = $"Unknown error while migrating database \"{_migrationConnection.DatabaseName}\"";

            _logger?.LogError(e, errorMessage);

            return MigrationResult.CreateFailed(
                MigrationErrorCode.UnknownError,
                errorMessage);
        }
    }

    /// <summary>
    /// Returns migrations to apply.
    /// </summary>
    /// <param name="isUpgrade">True if upgrade database, false if downgrade.</param>
    /// <param name="isPreMigration">Should we return pre-migrations?</param>
    /// <param name="token">Cancellation token.</param>
    private async Task<IReadOnlyList<IMigration>> GetMigrationsToApplyAsync(
        bool isUpgrade,
        bool isPreMigration,
        CancellationToken token)
    {
        var actionType = isUpgrade
            ? "upgrade"
            : "downgrade";
        var stageName = isPreMigration ? " (pre-migration stage)" : "";
        _logger?.LogInformation($"Getting migrations for {actionType}{stageName}...");

        var maxAvailableMigrationVersion = _availableMigrationsMap.Values.Max(x => x.Version);

        // get applied migrations versions
        var appliedMigrationVersions = await _migrationConnection.GetAppliedMigrationVersionsAsync(token);

        // provider should order migrations by version ascending, but we can't trust them, because provider can be external
        appliedMigrationVersions = appliedMigrationVersions.OrderBy(x => x).ToArray();

        var maxAppliedMigration = appliedMigrationVersions.LastOrDefault();

        _logger?.LogInformation($"Max available migration version - {maxAvailableMigrationVersion}. " +
                                $"Available migrations count - {_availableMigrationsMap.Count}");
        if (appliedMigrationVersions.Count == 0)
        {
            _logger?.LogInformation("No migrations have been applied yet");
        }
        else
        {
            _logger?.LogInformation($"Max applied migration version - {maxAppliedMigration}. " +
                                    $"Applied migrations count - {appliedMigrationVersions.Count}");
        }

        // build list of migrations to apply

        var availableMigrationVersions = new HashSet<MigrationVersion>(_availableMigrationsMap.Values.Select(x => x.Version));

        var migrationsToApply = new List<IMigration>();

        if (isUpgrade)
        {
            // we need only not applied migrations
            availableMigrationVersions.ExceptWith(appliedMigrationVersions);
            var query = availableMigrationVersions.AsEnumerable();
            if (_targetVersion.HasValue)
            {
                query = _onlyTargetVersion
                    ? query.Where(x => x == _targetVersion.Value)
                    : query.Where(x => x <= _targetVersion.Value);
            }

            var notAppliedMigrationVersions = query.OrderBy(x => x);

            foreach (var notAppliedMigrationVersion in notAppliedMigrationVersions)
            {
                var notAppliedMigration = _availableMigrationsMap[notAppliedMigrationVersion];
                migrationsToApply.Add(notAppliedMigration);
            }
        }
        else // it's downgrade
        {
            // we need only applied migrations
            // we do not check implementation of IDowngradableMigration here
            availableMigrationVersions.IntersectWith(appliedMigrationVersions);
            var query = availableMigrationVersions.AsEnumerable();
            if (_targetVersion.HasValue)
            {
                query = _onlyTargetVersion
                    ? query.Where(x => x == _targetVersion.Value)
                    : query.Where(x => x > _targetVersion.Value);
            }

            var migrationsVersionsToApply = query
                .OrderByDescending(x => x);

            foreach (var downgradeMigrationVersion in migrationsVersionsToApply)
            {
                var downgradeMigration = _availableMigrationsMap[downgradeMigrationVersion];
                migrationsToApply.Add(downgradeMigration);
            }
        }

        _logger?.LogInformation($"Getting migrations for {actionType}{stageName} completed. Found {migrationsToApply.Count} migrations to apply.");

        return migrationsToApply;
    }

    /// <summary>
    /// Executes pre-migration scripts.
    /// </summary>
    /// <returns>True if pre-migrations were executed, otherwise - false.</returns>
    /// <exception cref="MigrationException"></exception>
    private async Task<bool> ExecutePreMigrationScriptsAsync(CancellationToken token = default)
    {
        var desiredMigrations = _availablePreMigrations
            .OrderBy(x => x.Version)
            .ToArray();
        if (desiredMigrations.Length == 0)
            return false;

        foreach (var migration in desiredMigrations)
        {
            token.ThrowIfCancellationRequested();

            DbTransaction? transaction = null;

            await _migrationConnection.CloseConnectionAsync();
            await _migrationConnection.OpenConnectionAsync(token);

            try
            {
                _logger?.LogInformation(
                    $"Executing pre-migration script \"{migration.Version}\" (\"{migration.Comment}\") for database \"{_migrationConnection.DatabaseName}\"...");

                if (migration.IsTransactionRequired)
                {
                    transaction = _migrationConnection.BeginTransaction();
                }
                else
                {
                    _logger?.LogWarning($"Transaction is disabled for pre-migration {migration.Version}");
                }

                await migration.UpgradeAsync(transaction, token);

                transaction?.Commit();

                _logger?.LogInformation(
                    $"Executing pre-migration script \"{migration.Version}\" for database \"{_migrationConnection.DatabaseName}\" completed.");
            }
            catch (Exception e)
            {
                throw new MigrationException(
                    MigrationErrorCode.MigratingError,
                    $"Error while executing pre-migration to \"{migration.Version}\" for database \"{_migrationConnection.DatabaseName}\": {e.Message}",
                    e);
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        return true;
    }

    private async Task<(IReadOnlyList<MigrationInfo> Applied, IReadOnlyList<MigrationInfo> Skipped)> MigrateAsync(
        IReadOnlyList<IMigration> orderedMigrations,
        bool isUpgrade,
        MigrationPolicy policy,
        CancellationToken cancellationToken = default)
    {
        if (policy == MigrationPolicy.AllForbidden)
        {
            throw new MigrationException(
                MigrationErrorCode.PolicyError,
                $"{(isUpgrade ? "Upgrading" : "Downgrading")} is forbidden due to migration policy");
        }

        if (orderedMigrations.Count == 0) return (Array.Empty<MigrationInfo>(), Array.Empty<MigrationInfo>());

        var operationName = isUpgrade
            ? "Upgrade"
            : "Downgrade";

        var appliedMigrations = new List<MigrationInfo>(orderedMigrations.Count);
        var skippedMigrations = new List<MigrationInfo>();

        for (var i = 0; i < orderedMigrations.Count; i++)
        {
            var migration = orderedMigrations[i];
            var currentMigration = new MigrationInfo(migration.Version, migration.Comment);

            // check policies
            if (!policy.HasFlag(MigrationPolicy.LongRunningAllowed) && migration.IsLongRunning)
            {
                skippedMigrations.Add(currentMigration);
                _logger?.LogWarning($"Skip long-running migration \"{migration.Version}\" due to policy restriction");
                continue;
            }
            if (!policy.HasFlag(MigrationPolicy.ShortRunningAllowed) && !migration.IsLongRunning)
            {
                skippedMigrations.Add(currentMigration);
                _logger?.LogWarning($"Skip short-running migration \"{migration.Version}\" due to policy restriction");
                continue;
            }

            if (migration.Dependencies.Any())
            {
                if (!migration.Dependencies.TrueForAll(x =>
                        appliedMigrations.Any(aM =>
                            string.Equals(aM.Version.ToString(), x, StringComparison.OrdinalIgnoreCase))))
                {
                    throw new MigrationException(MigrationErrorCode.MigrationNotFound, 
                        $"Migration with version \"{migration.Version}\" depends on unapplied migrations \"{string.Join(" ", migration.Dependencies)}\"");
                }
            }

            DbTransaction? transaction = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // sometimes transactions fails without reopening connection
                //todo #19: fix it later
                await _migrationConnection.CloseConnectionAsync();
                await _migrationConnection.OpenConnectionAsync(cancellationToken);

                if (migration.IsTransactionRequired)
                {
                    transaction = _migrationConnection.BeginTransaction();
                }
                else
                {
                    _logger?.LogWarning($"Transaction is disabled for migration \"{migration.Version}\"");
                }

                _logger?.LogInformation($"{operationName} to \"{migration.Version}\" ({migration.Comment} for database \"{_migrationConnection.DatabaseName}\")...");

                if (isUpgrade)
                {
                    await migration.UpgradeAsync(transaction, cancellationToken);
                    await _migrationConnection.SaveAppliedMigrationVersionAsync(migration.Version, migration.Comment, cancellationToken);
                }
                else
                {
                    if (!(migration is IDowngradeMigration downgradableMigration))
                        throw new MigrationException(MigrationErrorCode.MigrationNotFound, $"Migration with version \"{migration.Version}\" doesn't support downgrade");

                    await downgradableMigration.DowngradeAsync(transaction, cancellationToken);
                    await _migrationConnection.DeleteAppliedMigrationVersionAsync(migration.Version, cancellationToken);
                }

                // Commit transaction if all commands succeed, transaction will auto-rollback
                // when disposed if either commands fails
                transaction?.Commit();

                appliedMigrations.Add(currentMigration);
                _logger?.LogInformation($"{operationName} to \"{migration.Version}\" (database \"{_migrationConnection.DatabaseName}\") completed.");
            }
            catch (MigrationException e)
            {
                throw new MigrationException(
                    e.ErrorCode,
                    e.Message,
                    e,
                    currentMigration);
            }
            catch (Exception e)
            {
                throw new MigrationException(
                    MigrationErrorCode.MigratingError,
                    $"Error while executing {operationName.ToLower()} migration to \"{migration.Version}\" for database \"{_migrationConnection.DatabaseName}\": {e.Message}",
                    e,
                    currentMigration);
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        return (appliedMigrations, skippedMigrations);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _migrationConnection.Dispose();
    }
}
