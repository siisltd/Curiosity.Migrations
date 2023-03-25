using System;

namespace Curiosity.Migrations.PostgreSQL;

/// <summary>
/// Factory of <see cref="PostgresMigrationConnection"/>
/// </summary>
public class PostgresMigrationConnectionFactory : IMigrationConnectionFactory
{
    private readonly PostgresMigrationConnectionOptions _options;

    /// <inheritdoc cref="PostgresMigrationConnectionFactory"/>
    public PostgresMigrationConnectionFactory(PostgresMigrationConnectionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public IMigrationConnection CreateMigrationConnection()
    {
        return new PostgresMigrationConnection(_options);
    }
}
