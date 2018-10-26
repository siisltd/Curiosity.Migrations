using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;

namespace Marvin.Migrations
{
    /// <summary>
    /// Migration configuration. Specify current DB info and provider to access DB
    /// </summary>
    public interface IMigrationConfiguration
    {
        /// <summary>
        /// Returns provider to access DB
        /// </summary>
        /// <returns></returns>
        IDbProvider GetProvider();

        /// <summary>
        /// Returns actual DB info
        /// </summary>
        /// <returns></returns>
        DbInfo GetInfo();
    }
}