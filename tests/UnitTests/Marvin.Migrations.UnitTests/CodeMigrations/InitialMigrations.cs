using System.Threading.Tasks;
using System.Transactions;

namespace Marvin.Migrations.UnitTests.CodeMigrations
{
    public class InitialMigration : CodeMigration
    {
        public override DbVersion Version { get; } = new DbVersion(1,0);
        public override string Comment { get; } = "comment";
        
        public InitialMigration(IDbProvider dbProvider) : base(dbProvider)
        {
        }


        public override Task UpgradeAsync(CommittableTransaction transaction)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.UpScript);
        }

        public override Task DowngradeAsync(CommittableTransaction transaction)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.DownScript);
        }
    }
}