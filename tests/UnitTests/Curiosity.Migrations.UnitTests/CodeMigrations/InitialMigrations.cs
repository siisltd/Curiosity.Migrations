using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Curiosity.Migrations.UnitTests.CodeMigrations
{
    public class InitialMigration : CodeMigration
    {
        public override DbVersion Version { get; } = new DbVersion(1,0);
        public override string Comment { get; } = "comment";
        

        public override Task UpgradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.UpScript, token);
        }

        public override Task DowngradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.DownScript, token);
        }
    }
}