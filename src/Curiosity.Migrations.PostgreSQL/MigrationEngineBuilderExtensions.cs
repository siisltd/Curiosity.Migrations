using System.Diagnostics.CodeAnalysis;

namespace Curiosity.Migrations.PostgreSQL;

/// <summary>
/// Extension to adding Postgre migrations to <see cref="MigrationEngineBuilder"/>
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
public static class MigrationEngineBuilderExtensions
{
    /// <summary>
    /// Use provider to make migration on PostgreSQL database. 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="connectionString">Connection string to PostgreSQL</param>
    /// <param name="migrationTableHistoryName">Migration history table name. If param <see langword="null"/> default value from DB will be used </param>
    /// <param name="databaseEncoding">Text presentation of database encoding for Postgre. If <see langword="null"/> default value from DB will be used </param>
    /// <param name="lcCollate"> String sort order for Postgres. If param <see langword="null"/> default value from DB will be used </param>
    /// <param name="lcCtype">Character classification for Postgres. If param <see langword="null"/> default value from DB will be used</param>
    /// <param name="connectionLimit">Limit of connections to Postgres. If param <see langword="null"/> default value from DB will be used </param>
    /// <param name="template">The name of the template from which to create the new database. If param <see langword="null"/> default value from DB will be used</param>
    /// <param name="tableSpace">The name of the tablespace that will be associated with the new database. If param <see langword="null"/> default value from DB will be used </param>
    /// <remarks>
    /// For detailed params description look at <see cref="PostgresMigrationConnectionOptions"/>
    /// </remarks>
    // ReSharper disable once InconsistentNaming
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static MigrationEngineBuilder ConfigureForPostgreSql(
        this MigrationEngineBuilder builder,
        string connectionString,
        string? migrationTableHistoryName = null,
        string? databaseEncoding = null,
        string? lcCollate = null,
        string? lcCtype = null,
        int? connectionLimit = null,
        string? template = null,
        string? tableSpace = null)
    {
        Guard.AssertNotNull(builder, nameof(builder));

        var options = new PostgresMigrationConnectionOptions(
            connectionString,
            migrationTableHistoryName,
            databaseEncoding,
            lcCollate,
            lcCtype,
            connectionLimit,
            template,
            tableSpace);
        builder.UseMigrationConnectionFactory(new PostgresMigrationConnectionFactory(options));
        return builder;
    }
}
