using System.Threading.Tasks;
using Marvin.Migrations.Migrations;

namespace Marvin.Migrations.UnitTests.CodeMigrations
{
    public class InitialMigration : CodeMigration
    {
        public override DbVersion Version { get; } = new DbVersion(1,0);
        public override string Comment { get; } = "comment";
        
        public InitialMigration(IDbProvider dbProvider) : base(dbProvider)
        {
        }


        public override Task UpgradeAsync()
        {
            return _dbProvider.ExecuteScriptAsync(ScriptConstants.UpScript);
        }

        public override Task DowngradeAsync()
        {
            return _dbProvider.ExecuteScriptAsync(ScriptConstants.DownScript);
        }
    }
}