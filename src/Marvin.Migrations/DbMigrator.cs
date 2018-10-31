using System;
using System.Linq;
using System.Threading.Tasks;
using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;
using Microsoft.Extensions.Logging;

namespace Marvin.Migrations.Migrators
{
    /// <summary>
    /// Default realisation of <see cref="IDbMigrator"/>
    /// </summary>
    public sealed class DbMigrator : IDbMigrator
    {
        private readonly AutoMigrationPolicy _upgradePolicy;
        private readonly AutoMigrationPolicy _downgradePolicy;

        private readonly ILogger _logger;

        /// <summary>
        /// Default realisation of <see cref="IDbMigrator"/>
        /// </summary>
        /// <param name="upgradePolicy"></param>
        /// <param name="downgradePolicy"></param>
        /// <param name="logger"></param>
        public DbMigrator(
            AutoMigrationPolicy upgradePolicy, 
            AutoMigrationPolicy downgradePolicy, 
            ILogger logger = null)
        {
            _upgradePolicy = upgradePolicy;
            _downgradePolicy = downgradePolicy;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task MigrateAsync(IDbProvider dbProvider, DbInfo dbInfo)
        {
            if (dbProvider == null) throw new ArgumentNullException(nameof(dbProvider));
            if (dbInfo == null) throw new ArgumentNullException(nameof(dbInfo));

            try
            {
                await dbProvider.CreateDatabaseIfNotExistsAsync().ConfigureAwait(false);
                await dbProvider.CreateHistoryTableIfNotExistsAsync().ConfigureAwait(false);
                var dbVersion = await dbProvider
                    .GetDbVersionAsync()
                    .ConfigureAwait(false)
                    ?? new DbVersion?(new DbVersion(0,0));
                if (dbInfo.ActualVersion > dbVersion.Value)
                {
                    _logger?.LogInformation($"Migrating database {dbProvider.DbName}...");

                    await Upgrade(dbProvider, dbInfo, dbVersion.Value).ConfigureAwait(false);

                    _logger?.LogInformation($"Migrating database {dbProvider.DbName} completed.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error while migrating database {dbProvider.DbName}", ex);
                throw;
            }
        }

        private async Task Upgrade(IDbProvider dbProvider, DbInfo info, DbVersion actualVersion)
        {
            var desiredMigrations = info.Migrations
                .Where(x => x.Version > actualVersion)
                .OrderBy(x => x.Version)
                .ToList();
            if (desiredMigrations.Count == 0) return;
            var targetVersion = desiredMigrations.Last().Version;
            var lastMigrationVersion = new DbVersion(0,0);
            foreach (var migration in desiredMigrations)
            {
                if (!IsMigrationAllowed(DbVersion.GetDifference(actualVersion, migration.Version)))
                {
                    return;
                }

                await dbProvider.ExecuteScriptAsync(migration.Script).ConfigureAwait(false);
                await dbProvider.UpdateCurrentDbVersionAsync(migration.Version).ConfigureAwait(false);
                lastMigrationVersion = migration.Version;
            }
            if (lastMigrationVersion != targetVersion) throw new InvalidOperationException(
                $"Can not migrate database to version {targetVersion}. Last executed migration is {lastMigrationVersion}");
        }
        
        //todo downgrade

        private bool IsMigrationAllowed(DbVersionDifference versionDifference)
        {
            switch (versionDifference)
            {
                case DbVersionDifference.Major:
                    return _upgradePolicy.HasFlag(AutoMigrationPolicy.Major);

                case DbVersionDifference.Minor:
                    return _upgradePolicy.HasFlag(AutoMigrationPolicy.Minor);

                default:
                    return false;
            }
        }
    }
}