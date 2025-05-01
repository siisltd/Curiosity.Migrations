using Curiosity.Migrations.SqlServer;
using Xunit;

namespace Curiosity.Migrations.SqlServer.UnitTests;

public class SqlServerGuard_Should
{
    [Fact]
    public void NotAssert_OnCorrectTableName()
    {
        SqlServerGuard.AssertTableName("migration_history", "table_name");
        SqlServerGuard.AssertTableName("pds_migration_history", "table_name");
        SqlServerGuard.AssertTableName("cds_migration_history", "table_name");
        SqlServerGuard.AssertTableName("ods_migration_history", "table_name");
        SqlServerGuard.AssertTableName("protocol_migration_history", "table_name");
        SqlServerGuard.AssertTableName("table", "table_name");
        SqlServerGuard.AssertTableName("table123", "table_name");
        SqlServerGuard.AssertTableName("TABLE-table", "table_name");
    }

    [Fact]
    public void Assert_OnInvalidTableName()
    {
        Assert.Throws<ArgumentNullException>(() => SqlServerGuard.AssertTableName("", "table_name"));
        Assert.Throws<ArgumentNullException>(() => SqlServerGuard.AssertTableName(null!, "table_name"));
        Assert.Throws<ArgumentNullException>(() => SqlServerGuard.AssertTableName(" ", "table_name"));
    }

    [Fact]
    public void NotAssert_OnCorrectConnectionString()
    {
        SqlServerGuard.AssertConnectionString("Server=localhost;Database=test;User Id=sa;Password=Password123;", "connectionString");
        SqlServerGuard.AssertConnectionString("Server=localhost,1433;Database=test;User Id=sa;Password=Password123;", "connectionString");
        SqlServerGuard.AssertConnectionString("Server=localhost\\SQLEXPRESS;Database=test;User Id=sa;Password=Password123;", "connectionString");
        SqlServerGuard.AssertConnectionString("Server=localhost\\SQLEXPRESS,1433;Database=test;User Id=sa;Password=Password123;", "connectionString");
    }

    [Fact]
    public void Assert_OnInvalidConnectionString()
    {
        Assert.Throws<ArgumentNullException>(() => SqlServerGuard.AssertConnectionString("", "connectionString"));
        Assert.Throws<ArgumentNullException>(() => SqlServerGuard.AssertConnectionString(null!, "connectionString"));
        Assert.Throws<ArgumentNullException>(() => SqlServerGuard.AssertConnectionString(" ", "connectionString"));
    }
} 