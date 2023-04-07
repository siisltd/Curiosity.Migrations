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

    private readonly IReadOnlyDictionary<MigrationVersion, IMigration> _availableMigrationsMap;
    private readonly IReadOnlyList<IMigration> _availablePreMigrations;

    /// <inheritdoc cref="MigrationEngine"/>
    /// <param name="migrationConnection">Provider for database</param>
    /// <param name="migrations">Main migrations for changing database</param>
    /// <param name="upgradePolicy">Policy for upgrading database</param>
    /// <param name="downgradePolicy">Policy for downgrading database</param>
    /// <param name="preMigrations">Migrations that will be executed before <paramref name="migrations"/></param>
    /// <param name="targetVersion">Desired version of database after migration. If <see langword="null"/> migrator will upgrade database to the most actual version.</param>
    /// <param name="logger">Optional logger</param>
    public MigrationEngine(
        IMigrationConnection migrationConnection,
        IReadOnlyList<IMigration> migrations,
        MigrationPolicy upgradePolicy,
        MigrationPolicy downgradePolicy,
        IReadOnlyList<IMigration>? preMigrations = null,
        MigrationVersion? targetVersion = null,
        ILogger? logger = null)
    {
        Guard.AssertNotNull(migrationConnection, nameof(migrationConnection));
        Guard.AssertNotNull(migrations, nameof(migrations));

        _migrationConnection = migrationConnection;
        _logger = logger;

        var migrationMap = new Dictionary<MigrationVersion, IMigration>();
        foreach (var migration in migrations)
        {
            if (migrationMap.ContainsKey(migration.Version))
                throw new ArgumentException(
                    $"There is more than one migration with version {migration.Version}", nameof(migrations));

            migrationMap.Add(migration.Version, migration);
        }

        _availableMigrationsMap = migrationMap;

        _upgradePolicy = upgradePolicy;
        _downgradePolicy = downgradePolicy;
        _targetVersion = targetVersion;

        _availablePreMigrations = preMigrations ?? Array.Empty<IMigration>();
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
    public async Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default)
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
                _logger?.LogInformation($"Table \"{_migrationConnection.MigrationHistoryTableName}\" exists. ");
            }
            else
            {
                _logger?.LogInformation($"Creating \"{_migrationConnection.MigrationHistoryTableName}\" table...");
                await _migrationConnection.CreateMigrationHistoryTableIfNotExistsAsync(cancellationToken);
                _logger?.LogInformation($"Creating \"{_migrationConnection.MigrationHistoryTableName}\" table completed.");
            }

            // get applied migrations versions
            var (isUpgrade, migrationsToApply) = await GetMigrationsToApplyAsync(false, cancellationToken);

            if (migrationsToApply.Count == 0)
            {
                _logger?.LogInformation($"Database \"{_migrationConnection.DatabaseName}\" is actual. Stop migration.");
                return MigrationResult.CreateSuccessful(
                    Array.Empty<MigrationInfo>(),
                    Array.Empty<MigrationInfo>());
            }

            _logger?.LogInformation($"Executing pre-migration scripts for database \"{_migrationConnection.DatabaseName}\"...");
            var wasPreMigrationExecuted = await ExecutePreMigrationScriptsAsync(cancellationToken);
            if (wasPreMigrationExecuted)
            {
                _logger?.LogInformation($"Executing pre-migration scripts for database \"{_migrationConnection.DatabaseName}\" completed.");

                // applied migration versions might be changed after pre-migration
                (isUpgrade, migrationsToApply) = await GetMigrationsToApplyAsync(true, cancellationToken);
            }
            else
            {
                _logger?.LogInformation("No pre-migration scripts were found");
            }

            if (migrationsToApply.Count == 0)
            {
                _logger?.LogInformation($"Database \"{_migrationConnection.DatabaseName}\" is actual. Stop migration.");
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
    /// <param name="isPreMigration">Should we return pre-migrations?</param>
    /// <param name="token">Cancellation token.</param>
    private async Task<(bool isUpgrade, IReadOnlyList<IMigration> migrations)> GetMigrationsToApplyAsync(
        bool isPreMigration,
        CancellationToken token)
    {
        var stageName = isPreMigration ? " (pre-migration stage)" : "";
        _logger?.LogInformation($"Getting migrations to apply{stageName}...");

        // build target version
        var maxAvailableMigrationVersion = _availableMigrationsMap.Values.Max(x => x.Version);

        // get applied migrations versions
        var appliedMigrationVersions = await _migrationConnection.GetAppliedMigrationVersionsAsync(token);

        // provider should order migrations by version ascending, but we can't trust them, because it can be external
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

        bool isUpgrade;
        // if no target version is specified or target version is greater than max applied
        if (!_targetVersion.HasValue || _targetVersion.Value >= maxAppliedMigration)
        {
            // we need only not applied migrations
            availableMigrationVersions.ExceptWith(appliedMigrationVersions);
            var query = availableMigrationVersions.AsEnumerable();
            if (_targetVersion.HasValue)
            {
                query = query.Where(x => x <= _targetVersion.Value);
            }

            var notAppliedMigrationVersions = query.OrderBy(x => x);

            foreach (var notAppliedMigrationVersion in notAppliedMigrationVersions)
            {
                var notAppliedMigration = _availableMigrationsMap[notAppliedMigrationVersion];
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
                .Where(x => x > _targetVersion.Value)
                .OrderByDescending(x => x);

            foreach (var downgradeMigrationVersion in migrationsVersionsToApply)
            {
                var downgradeMigration = _availableMigrationsMap[downgradeMigrationVersion];
                migrationsToApply.Add(downgradeMigration);
            }

            isUpgrade = false;
        }

        _logger?.LogInformation($"Getting migrations to apply{stageName} completed. Found {migrationsToApply.Count} migrations to apply.");

        return (isUpgrade, migrationsToApply);
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
            }
            if (!policy.HasFlag(MigrationPolicy.ShortRunningAllowed) && !migration.IsLongRunning)
            {
                skippedMigrations.Add(currentMigration);
                _logger?.LogWarning($"Skip short-running migration \"{migration.Version}\" due to policy restriction");
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
