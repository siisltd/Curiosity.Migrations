using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Migration that supports downgrade
    /// </summary>
    public interface IDowngradeMigration : IMigration
    {
        /// <summary>
        /// Downgrade database to the previous version undoing changes to this migration
        /// </summary>
        /// <returns></returns>
        Task DowngradeAsync(DbTransaction transaction, CancellationToken token = default);
    }
}