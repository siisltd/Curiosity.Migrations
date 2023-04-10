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

    /// <inheritdoc />
    public MigrationException(
        MigrationErrorCode errorCode,
        string message,
        MigrationInfo? migrationInfo = null) : base(message)
    {
        ErrorCode = errorCode;
        MigrationInfo = migrationInfo;
    }

    /// <inheritdoc />
    public MigrationException(
        MigrationErrorCode errorCode,
        string message,
        Exception inner,
        MigrationInfo? migrationInfo = null) : base(message, inner)
    {
        ErrorCode = errorCode;
        MigrationInfo = migrationInfo;
    }
}
