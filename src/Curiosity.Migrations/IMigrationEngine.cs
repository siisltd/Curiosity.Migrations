using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations;

/// <summary>
/// Engine that executes configured migrations.
/// </summary>
public interface IMigrationEngine
{
    Task<MigrationResult> UpgradeDatabaseAsync(CancellationToken cancellationToken = default);

    Task<MigrationResult> DowngradeDatabaseAsync(CancellationToken cancellationToken = default);
}
