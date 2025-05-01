namespace Curiosity.Migrations.MsSql;

/// <summary>
/// Factory of <see cref="MsSqlMigrationConnection"/>
/// </summary>
public class MsSqlMigrationConnectionFactory : IMigrationConnectionFactory
{
    private readonly MsSqlMigrationConnectionOptions _options;

    /// <inheritdoc cref="MsSqlMigrationConnectionFactory"/>
    public MsSqlMigrationConnectionFactory(MsSqlMigrationConnectionOptions options)
    {
        Guard.AssertNotNull(options, nameof(options));

        _options = options;
    }

    /// <inheritdoc />
    public IMigrationConnection CreateMigrationConnection()
    {
        return new MsSqlMigrationConnection(_options);
    }
} 