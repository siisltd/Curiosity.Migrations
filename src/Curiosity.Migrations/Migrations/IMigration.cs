using System.Data.Common;
using System.Threading;
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

        bool IsTransactionRequired { get; }

        /// <summary>
        /// Upgrade database to the version specified in <see cref="Version"/>
        /// </summary>
        /// <param name="transaction">Transaction in which operation must be executed. Optional. Use it when you need attach transaction to Entity Framework data context.</param>
        /// <param name="token">Cancellation token. Optional.</param>
        /// <returns>Task associated with upgrade</returns>
        Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken token = default);
    }
}