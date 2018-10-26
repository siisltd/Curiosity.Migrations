using System.Threading.Tasks;
using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;

namespace Marvin.Migrations.Migrators
{
    /// <summary>
    /// Мигратор базы
    /// </summary>
    public interface IDbMigrator
    {
        Task MigrateAsync(IDbProvider dbProvider, DbInfo dbInfo);
    }
}