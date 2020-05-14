using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations.UnitTests.CodeMigrations
{
    public class FourthMigrationWithDependency : CustomBaseCodeMigration, IDowngradeMigration
    {
        public DependencyService DependencyService { get; }

        public override DbVersion Version { get; } = new DbVersion(1,3);
        
        public override string Comment { get; } = "comment";

        public FourthMigrationWithDependency(DependencyService dependencyService)
        {
            DependencyService = dependencyService ?? throw new ArgumentNullException(nameof(dependencyService));
        }


        public override Task UpgradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.UpScript, token);
        }

        public Task DowngradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.DownScript, token);
        }
    }
}
