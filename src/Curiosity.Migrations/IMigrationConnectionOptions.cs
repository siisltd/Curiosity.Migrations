// ReSharper disable UnusedMemberInSuper.Global
namespace Curiosity.Migrations;

/// <summary>
/// Options for <see cref="IMigrationConnection"/>
/// </summary>
public interface IMigrationConnectionOptions
{
    /// <summary>
    /// Connection string to a database.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    /// Name of migration history table.
    /// </summary>
    /// <remarks>
    /// If property is <see langword="null"/> <see cref="IMigrationConnection"/> will used default value
    /// </remarks>
    string? MigrationHistoryTableName { get; }
}
