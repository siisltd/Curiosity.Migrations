namespace Curiosity.Migrations;

/// <summary>
/// Migration error code.
/// </summary>
public enum MigrationErrorCode
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Can not connect to database.
    /// </summary>
    /// <remarks>
    /// Incorrect server address, port, network problems, etc.
    /// </remarks>
    ConnectionError = 10,

    /// <summary>
    /// Can not authorize.
    /// </summary>
    /// <remarks>
    /// Incorrect login, password.
    /// </remarks>
    AuthorizationError = 20,

    /// <summary>
    /// Can not create database.
    /// </summary>
    /// <remarks>
    /// Incorrect creation script on user does no have permission to create databases.
    /// </remarks>
    CreatingDbError = 30,

    /// <summary>
    /// Can not create migration history table.
    /// </summary>
    /// <remarks>
    /// Incorrect creation script on user does no have permission to create databases.
    /// </remarks>
    CreatingHistoryTable = 50,

    /// <summary>
    /// Error during executing migrations commands.
    /// </summary>
    /// <remarks>
    /// Incorrect script.
    /// </remarks>
    MigratingError = 70,

    /// <summary>
    /// Migration with specified <see cref="DbVersion"/> not found.
    /// </summary>
    /// <remarks>
    /// Check loaded migration from file, assembly, etc.
    /// </remarks>
    MigrationNotFound = 90,

    /// <summary>
    /// Policy restricts migration.
    /// </summary>
    /// <remarks>
    /// Check <see cref="MigrationPolicy"/>.
    /// </remarks>
    PolicyError = 100
}
