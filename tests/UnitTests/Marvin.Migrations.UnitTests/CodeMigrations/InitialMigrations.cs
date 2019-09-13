using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;

namespace Marvin.Migrations.UnitTests.CodeMigrations
{
    public class InitialMigration : CodeMigration
    {
        public override DbVersion Version { get; } = new DbVersion(1,0);
        public override string Comment { get; } = "comment";
        
        public InitialMigration(IDbProvider dbProvider) : base(dbProvider, new Dictionary<string, string>())
        {
        }


        public override Task UpgradeAsync(DbTransaction transaction)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.UpScript);
        }

        public override Task DowngradeAsync(DbTransaction transaction)
        {
            return DbProvider.ExecuteScriptAsync(ScriptConstants.DownScript);
        }
    }
}