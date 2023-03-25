using System;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Curiosity.Migrations
{
    /// <summary>
    /// Result of migration
    /// </summary>
    public readonly struct MigrationResult
    {
        /// <summary>
        /// If migration complete successfully
        /// </summary>
        public bool IsSuccessfully => ErrorCode == null;

        /// <summary>
        /// Error occured during migration
        /// </summary>
        public MigrationErrorCode? ErrorCode { get; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Count of applied migrations during this migrator's running.
        /// </summary>
        public int AppliedMigrationsCount { get; }


        private MigrationResult(
            MigrationErrorCode? errorCode,
            string errorMessage,
            int appliedMigrationsCount)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            AppliedMigrationsCount = appliedMigrationsCount;
        }

        /// <summary>
        /// Create successfully migration result
        /// </summary>
        /// <returns></returns>
        public static MigrationResult SuccessfullyResult(int appliedMigrationsCount)
        {
            if (appliedMigrationsCount < 0) throw new ArgumentOutOfRangeException(nameof(appliedMigrationsCount));

            return new MigrationResult(null, String.Empty, appliedMigrationsCount);
        }

        /// <summary>
        /// Create failure migration result with specified params
        /// </summary>
        /// <param name="errorCode">Migration error code</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns></returns>
        public static MigrationResult FailureResult(MigrationErrorCode errorCode, string errorMessage)
        {
            Guard.AssertNotEmpty(errorMessage, nameof(errorMessage));

            return new MigrationResult(errorCode, errorMessage, 0);
        }
    }
}
