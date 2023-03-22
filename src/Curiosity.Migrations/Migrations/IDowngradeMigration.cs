using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations;

/// <summary>
/// Migration that supports downgrade/
/// </summary>
public interface IDowngradeMigration : IMigration
{
    /// <summary>
    /// Downgrades database to the previous version undoing changes of this migration.
    /// </summary>
    Task DowngradeAsync(DbTransaction? transaction = null, CancellationToken token = default);
}
