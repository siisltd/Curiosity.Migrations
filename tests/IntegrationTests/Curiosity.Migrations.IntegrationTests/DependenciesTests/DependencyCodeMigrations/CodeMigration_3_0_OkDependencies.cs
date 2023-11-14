using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations.IntegrationTests.DependenciesTests.DependencyCodeMigrations;

public class CodeMigration_3_0_OkDependencies : CodeMigration, IDependencyMigration
{
    /// <inheritdoc />
    public override MigrationVersion Version { get; } = new(3);

    /// <inheritdoc />
    public override string Comment { get; } = "Migration using multiple EF context with one connection";

    public CodeMigration_3_0_OkDependencies()
    {
        Dependencies = new List<MigrationVersion>() { new(1,0), new(2,0) };
    }
    
    /// <inheritdoc />
    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync("select 1;", null, cancellationToken);
    }
}
