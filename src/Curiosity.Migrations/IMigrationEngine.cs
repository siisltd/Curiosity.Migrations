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
    /// <returns>Count of applied migrations.</returns>
    Task<MigrationResult> MigrateAsync(CancellationToken cancellationToken = default);
}
