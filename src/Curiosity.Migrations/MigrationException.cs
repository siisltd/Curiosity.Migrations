using System;

namespace Curiosity.Migrations;

/// <summary>
/// Exception occured during migration
/// </summary>
internal class MigrationException : Exception
{
    /// <summary>
    /// Code of migration error
    /// </summary>
    public MigrationErrorCode ErrorCode { get; }

    /// <summary>
    /// Migration that resulted in the exception.
    /// </summary>
    public MigrationInfo? MigrationInfo { get; }

    /// <summary>
    /// Name of database where exception was thrown.
    /// </summary>
    public string? DatabaseName { get; }
    
    /// <inheritdoc />
    public MigrationException(
        MigrationErrorCode errorCode,
        string message,
        string? databaseName = null,
        MigrationInfo? migrationInfo = null) : base(message)
    {
        DatabaseName = databaseName;
        ErrorCode = errorCode;
        MigrationInfo = migrationInfo;
    }

    /// <inheritdoc />
    public MigrationException(
        MigrationErrorCode errorCode,
        string message,
        Exception inner,
        string? databaseName = null,
        MigrationInfo? migrationInfo = null) : base(message, inner)
    {
        DatabaseName = databaseName;
        ErrorCode = errorCode;
        MigrationInfo = migrationInfo;
    }
}
