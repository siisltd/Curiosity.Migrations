using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;

namespace Marvin.Migrations.TransactionTests.CodeMigrations
{
    public class CodeMigration_2_0 : CodeMigration
    {
        public CodeMigration_2_0(IDbProvider dbProvider, IReadOnlyDictionary<string, string> variables) : base(dbProvider, variables)
        {
        }

        public override DbVersion Version { get; } = new DbVersion(2, 0);

        public override string Comment { get; } = "Correct script via provider";
        
        public override async Task UpgradeAsync(DbTransaction transaction)
        {
            await DbProvider.ExecuteScriptAsync("select 1;");
        }

        public override Task DowngradeAsync(DbTransaction transaction)
        {
            return Task.CompletedTask;
        }
    }
}