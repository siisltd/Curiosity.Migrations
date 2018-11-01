using System.Threading.Tasks;
using Marvin.Migrations.Migrations;

namespace Marvin.Migrations.UnitTests.CodeMigrations
{
    public class ThirdMigration : CodeMigration, ISpecificCodeMigrations
    {
        public ThirdMigration(IDbProvider dbProvider) : base(dbProvider)
        {
        }

        public override DbVersion Version { get; } = new DbVersion(1,2);
        
        public override string Comment { get; } = "comment";
        
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