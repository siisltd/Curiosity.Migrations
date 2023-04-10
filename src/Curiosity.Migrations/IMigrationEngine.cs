using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations;

/// <summary>
/// Engine that executes configured migrations.
/// </summary>
public interface IMigrationEngine
{
    /// <summary>
    /// Upgrades database according to migration configuration.
    /// </summary>
    Task<MigrationResult> UpgradeDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downgrades database according to migration configuration.
    /// </summary>
    Task<MigrationResult> DowngradeDatabaseAsync(CancellationToken cancellationToken = default);
}
