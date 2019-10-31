using System.Collections.Generic;

namespace Marvin.Migrations.UnitTests.CodeMigrations
{
    public abstract class CustomBaseCodeMigration : CodeMigration
    {
        protected CustomBaseCodeMigration(IDbProvider dbProvider, IReadOnlyDictionary<string, string> variables) : base(dbProvider, variables)
        {
        }
    }
}