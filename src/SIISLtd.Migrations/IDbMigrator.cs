using System.Threading.Tasks;

namespace SIISLtd.Migrations
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