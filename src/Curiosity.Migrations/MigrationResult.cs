using System;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Curiosity.Migrations
{
    /// <summary>
    /// Result of migration
    /// </summary>
    public class MigrationResult
    {
        /// <summary>
        /// If migration complete successfully
        /// </summary>
        public bool IsSuccessfully => Error == null;

        /// <summary>
        /// Error occured during migration
        /// </summary>
        public MigrationErrorCode? Error { get; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Count of applied migrations during this migrator's running.
        /// </summary>
        public int AppliedMigrationsCount { get; }

        private MigrationResult(MigrationErrorCode? error, string errorMessage, int appliedMigrationsCount)
        {
            Error = error;
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
            return new MigrationResult(errorCode, errorMessage, 0);
        }
    }
}
