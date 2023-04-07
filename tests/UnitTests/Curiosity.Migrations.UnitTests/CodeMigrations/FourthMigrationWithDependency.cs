using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations.UnitTests.CodeMigrations;

public class FourthMigrationWithDependency : CustomBaseCodeMigration, IDowngradeMigration
{
    public DependencyService DependencyService { get; }

    /// <inheritdoc />
    public override MigrationVersion Version { get; } = new(1,3);
        
    /// <inheritdoc />
    public override string Comment => "comment";

    public FourthMigrationWithDependency(DependencyService dependencyService)
    {
        DependencyService = dependencyService ?? throw new ArgumentNullException(nameof(dependencyService));
    }


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