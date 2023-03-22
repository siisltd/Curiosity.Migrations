using System;

namespace Curiosity.Migrations;

/// <summary>
/// Exception occured during migration
/// </summary>
public class MigrationException : Exception
{
    /// <summary>
    /// Code of migration error
    /// </summary>
    public MigrationErrorCode ErrorCode { get; }

    /// <inheritdoc />
    public MigrationException(MigrationErrorCode errorCode)
    {
        ErrorCode = errorCode;
    }

    /// <inheritdoc />
    public MigrationException(MigrationErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <inheritdoc />
    public MigrationException(MigrationErrorCode errorCode, string message, Exception inner) : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}