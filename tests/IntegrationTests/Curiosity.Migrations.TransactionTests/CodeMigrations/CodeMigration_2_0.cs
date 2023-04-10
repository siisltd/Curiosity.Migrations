using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations.TransactionTests.CodeMigrations;

public class CodeMigration_2_0 : CodeMigration
{
    /// <inheritdoc />
    public override MigrationVersion Version { get; } = new(2);

    /// <inheritdoc />
    public override string Comment => "Correct script via provider";

    /// <inheritdoc />
    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken token = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync("select 1;", null, token);
    }
}
