namespace Curiosity.Migrations.PostgreSQL.UnitTests;

public class PostgresqlGuard_Should
{
    [Fact]
    public void NotAssert_OnCorrectTableName()
    {
        PostgresqlGuard.AssertTableName("migration_history", "table_name");
        PostgresqlGuard.AssertTableName("pds_migration_history", "table_name");
        PostgresqlGuard.AssertTableName("cds_migration_history", "table_name");
        PostgresqlGuard.AssertTableName("ods_migration_history", "table_name");
        PostgresqlGuard.AssertTableName("protocol_migration_history", "table_name");
        PostgresqlGuard.AssertTableName("table", "table_name");
        PostgresqlGuard.AssertTableName("table123", "table_name");
        PostgresqlGuard.AssertTableName("TABLE-table", "table_name");
    }
}
