using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations;

/// <summary>
/// Migration to apply to a database.
/// </summary>
public interface IMigration
{
    /// <summary>
    /// Migration's version.
    /// </summary>
    MigrationVersion Version { get; }

    /// <summary>
    /// Migration's comment.
    /// </summary>
    string? Comment { get; }

    /// <summary>
    /// Is transaction required for this migration?
    /// </summary>
    /// <remarks>
    /// If true, migration engine will create separate transaction for this migration.
    /// If false, engine will not do that. Useful, when you need to manually open transactions or when you create index concurrently.
    /// </remarks>
    bool IsTransactionRequired { get; }

    /// <summary>
    /// It this migration long running?
    /// </summary>
    /// <remarks>
    ///
    /// </remarks>
    bool IsLongRunning { get; }

    /// <summary>
    /// Upgrades database to the version specified in <see cref="Version"/>.
    /// </summary>
    /// <param name="transaction">Transaction in which operation must be executed. Optional. Use it when you need attach transaction to Entity Framework data context.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>Task associated with upgrade.</returns>
    Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken token = default);
}
