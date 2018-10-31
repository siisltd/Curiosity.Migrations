using System.Threading.Tasks;
using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;

namespace Marvin.Migrations.Migrators
{
    /// <summary>
    /// Database migrator
    /// </summary>
    public interface IDbMigrator
    {
        /// <summary>
        /// Execute migration
        /// </summary>
        /// <param name="dbProvider">Database provider</param>
        /// <param name="dbInfo">Info about database</param>
        /// <returns></returns>
        Task MigrateAsync(IDbProvider dbProvider, DbInfo dbInfo);
    }
}