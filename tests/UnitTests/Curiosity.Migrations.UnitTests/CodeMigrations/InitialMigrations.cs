using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations.UnitTests.CodeMigrations;

public class InitialMigration : CodeMigration, IDowngradeMigration
{
    /// <inheritdoc />
    public override DbVersion Version { get; } = new(1);
    /// <inheritdoc />
    public override string Comment => "comment";

    /// <inheritdoc />
    public override Task UpgradeAsync(DbTransaction transaction, CancellationToken token = default)
    {
        return MigrationConnection.ExecuteNonQuerySqlAsync(
            ScriptConstants.UpScript,
            null,
            token);
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