using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Database migrator
    /// </summary>
    public interface IDbMigrator
    {
        /// <summary>
        /// Execute migration
        /// </summary>
        /// <exception cref="MigrationException"></exception>
        /// <returns>Count of applied migrations.</returns>
        Task<int> MigrateAsync(CancellationToken token = default);

        /// <summary>
        /// Execute migration without throwing exception
        /// </summary>
        Task<MigrationResult> MigrateSafeAsync(CancellationToken token = default);
    }
}
