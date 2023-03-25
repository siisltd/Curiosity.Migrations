using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations;

/// <summary>
/// Engine that executes configured migrations.
/// </summary>
public interface IMigrationEngine
{
    /// <summary>
    /// Executes migration of a database applying all migration according to migrator's configuration.
    /// </summary>
    /// <exception cref="MigrationException"></exception>
    /// <returns>Count of applied migrations.</returns>
    Task<int> MigrateAsync(CancellationToken token = default);

    /// <summary>
    /// Execute migration without throwing exception
    /// </summary>
    Task<MigrationResult> MigrateSafeAsync(CancellationToken token = default);
}
