using System;
using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Curiosity.Migrations;

/// <summary>
/// Result of migration engine work.
/// </summary>
public readonly struct MigrationResult
{
    /// <summary>
    /// If migration complete successfully
    /// </summary>
    public bool IsSuccessfully => ErrorCode == null;

    /// <summary>
    /// Error occured during migration.
    /// </summary>
    public MigrationErrorCode? ErrorCode { get; }

    /// <summary>
    /// Error message if any migration failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Information about migrations applied during engine work.
    /// </summary>
    public IReadOnlyList<MigrationInfo> AppliedMigrations { get; }

    /// <summary>
    /// Information about migrations skipped by policy during engine work.
    /// </summary>
    public IReadOnlyList<MigrationInfo> SkippedByPolicyMigrations { get; }

    /// <summary>
    /// Information about failed migration.
    /// </summary>
    public MigrationInfo? FailedMigration { get; }

    /// <inheritdoc cref="MigrationResult"/>
    private MigrationResult(
        IReadOnlyList<MigrationInfo>? appliedMigrations,
        IReadOnlyList<MigrationInfo>? skippedByPolicyMigrations,
        MigrationInfo? failedMigration,
        MigrationErrorCode? errorCode,
        string? errorMessage)
    {
        AppliedMigrations = appliedMigrations ?? Array.Empty<MigrationInfo>();
        SkippedByPolicyMigrations = skippedByPolicyMigrations ?? Array.Empty<MigrationInfo>();

        FailedMigration = failedMigration;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Create successfully migration result
    /// </summary>
    /// <returns></returns>
    public static MigrationResult CreateSuccessful(
        IReadOnlyList<MigrationInfo> appliedMigrations,
        IReadOnlyList<MigrationInfo> skippedByPolicyMigrations)
    {
        Guard.AssertNotNull(appliedMigrations, nameof(appliedMigrations));
        Guard.AssertNotNull(appliedMigrations, nameof(skippedByPolicyMigrations));

        return new MigrationResult(
            appliedMigrations,
            skippedByPolicyMigrations,
            null,
            null,
            null);
    }

    /// <summary>
    /// Create failure migration result with specified params
    /// </summary>
    /// <param name="errorCode">Migration error code</param>
    /// <param name="errorMessage">Error message</param>
    /// <param name="failedMigration">Information about failed migration.</param>
    /// <returns></returns>
    public static MigrationResult CreateFailed(
        MigrationErrorCode errorCode,
        string errorMessage,
        MigrationInfo? failedMigration = null)
    {
        Guard.AssertNotEmpty(errorMessage, nameof(errorMessage));

        return new MigrationResult(
            null,
            null,
            failedMigration,
            errorCode,
            errorMessage);
    }
}
