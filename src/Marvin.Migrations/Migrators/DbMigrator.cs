using System;
using System.Linq;
using System.Threading.Tasks;
using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;
using Microsoft.Extensions.Logging;

namespace Marvin.Migrations.Migrators
{
    public sealed class DbMigrator : IDbMigrator
    {
        private readonly AutoMigrationPolicy _policy;

        private readonly ILogger _logger;

        public DbMigrator(
            AutoMigrationPolicy policy, 
            ILogger logger = null)
        {
            _policy = policy;
            _logger = logger;
        }

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
                    _logger?.LogInformation($"{GetType().Name}: Миграция базы {dbProvider.DbName}...");

                    await Upgrade(dbProvider, dbInfo, dbVersion.Value).ConfigureAwait(false);

                    _logger?.LogInformation($"{GetType().Name}: Миграция базы {dbProvider.DbName} закончена.");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Произошла ошибка миграции базы {dbProvider.DbName}.", ex);
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
            var targerVersion = desiredMigrations.Last().Version;
            var lastMigrationVersion = new DbVersion(0,0);
            foreach (var migration in desiredMigrations)
            {
                if (!CheckPolicyAllowsMigration(DbVersion.GetDifference(actualVersion, migration.Version)))
                {
                    return;
                }

                await dbProvider.ExecuteScriptAsync(migration.Script).ConfigureAwait(false);
                await dbProvider.UpdateCurrentDbVersionAsync(migration.Version).ConfigureAwait(false);
                lastMigrationVersion = migration.Version;
            }
            if (lastMigrationVersion != targerVersion) throw new InvalidOperationException(
                $"Не удалось обновиться до требуемой версии {targerVersion}. Последний обработанный скрипт обновления {lastMigrationVersion}");
        }

        private bool CheckPolicyAllowsMigration(DbVersionDifference versionDifference)
        {
            switch (versionDifference)
            {
                case DbVersionDifference.Major:
                    return _policy.HasFlag(AutoMigrationPolicy.Major);

                case DbVersionDifference.Minor:
                    return _policy.HasFlag(AutoMigrationPolicy.Minor);


                default:
                    return false;
            }
        }
    }
}