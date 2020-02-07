using System;

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
        public MigrationError? Error { get; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; }

        private MigrationResult(MigrationError? error, string errorMessage)
        {
            Error = error;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Create successfully migration result
        /// </summary>
        /// <returns></returns>
        public static MigrationResult SuccessfullyResult()
        {
            return new MigrationResult(null, String.Empty);
        }

        /// <summary>
        /// Create failure migration result with specified params
        /// </summary>
        /// <param name="error">Migration error code</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns></returns>
        public static MigrationResult FailureResult(MigrationError error, string errorMessage)
        {
            return new MigrationResult(error, errorMessage);
        }
    }
}