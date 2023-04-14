using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations.UnitTests.CodeMigrations;

public class InitialMigration : CodeMigration, IDowngradeMigration
{
    /// <inheritdoc />
    public override MigrationVersion Version { get; } = new(1);
    /// <inheritdoc />
    public override string Comment => "comment";

    /// <inheritdoc />
    public override Task UpgradeAsync(DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        return MigrationConnection.ExecuteNonQuerySqlAsync(
            ScriptConstants.UpScript,
            null,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task DowngradeAsync(DbTransaction transaction, CancellationToken token = default)
    {
        return MigrationConnection.ExecuteNonQuerySqlAsync(
            ScriptConstants.DownScript,
            null,
            token);
    }
}