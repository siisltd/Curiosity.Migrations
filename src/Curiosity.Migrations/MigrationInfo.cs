namespace Curiosity.Migrations;

/// <summary>
/// Migrations' info.
/// </summary>
public struct MigrationInfo
{
    /// <summary>
    /// Migration's version.
    /// </summary>
    public DbVersion Version { get; }

    /// <summary>
    /// Migration's comment.
    /// </summary>
    public string? Comment { get; }

    /// <inheritdoc cref="MigrationInfo"/>
    public MigrationInfo(DbVersion version, string? comment)
    {
        Version = version;
        Comment = comment;
    }
}
