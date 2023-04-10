namespace Curiosity.Migrations;

/// <summary>
/// Factory of <see cref="IMigrationConnection"/>
/// </summary>
public interface IMigrationConnectionFactory
{
    /// <summary>
    /// Creates new instance of migration connection that implements <see cref="IMigrationConnection"/>
    /// </summary>
    IMigrationConnection CreateMigrationConnection();
}
