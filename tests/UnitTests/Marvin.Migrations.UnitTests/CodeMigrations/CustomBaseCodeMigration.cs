using Marvin.Migrations.Migrations;

namespace Marvin.Migrations.UnitTests.CodeMigrations
{
    public abstract class CustomBaseCodeMigration : CodeMigration
    {
        protected CustomBaseCodeMigration(IDbProvider dbProvider) : base(dbProvider)
        {
        }
    }
}