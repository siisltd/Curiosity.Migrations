using System.Data.Common;
using System.Threading.Tasks;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Migration
    /// </summary>
    public interface IMigration
    {
        /// <summary>
        /// Migration version
        /// </summary>
        DbVersion Version { get; }

        /// <summary>
        /// Migration comment
        /// </summary>
        string Comment { get; }

        /// <summary>
        /// Upgrade database to the version specified in <see cref="Version"/>
        /// </summary>
        /// <returns></returns>
        Task UpgradeAsync(DbTransaction transaction);

        /// <summary>
        /// Downgrade database to the previous version undoing changes to this migration
        /// </summary>
        /// <returns></returns>
        Task DowngradeAsync(DbTransaction transaction);
    }
}