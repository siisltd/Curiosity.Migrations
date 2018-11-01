using System.Threading.Tasks;
using Marvin.Migrations.Exceptions;
using Marvin.Migrations.Info;

namespace Marvin.Migrations
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
        Task MigrateAsync();
        
        /// <summary>
        /// Execute migration without throwing exception
        /// </summary>
        Task<MigrationResult> MigrateSafeAsync();
    }
}