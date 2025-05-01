namespace Curiosity.Migrations.SqlServer;

/// <summary>
/// Factory of <see cref="SqlServerMigrationConnection"/>
/// </summary>
public class SqlServerMigrationConnectionFactory : IMigrationConnectionFactory
{
    private readonly SqlServerMigrationConnectionOptions _options;

    /// <inheritdoc cref="SqlServerMigrationConnectionFactory"/>
    public SqlServerMigrationConnectionFactory(SqlServerMigrationConnectionOptions options)
    {
        Guard.AssertNotNull(options, nameof(options));
        _options = options;
    }

    /// <inheritdoc />
    public IMigrationConnection CreateMigrationConnection()
    {
        return new SqlServerMigrationConnection(_options);
    }
} 