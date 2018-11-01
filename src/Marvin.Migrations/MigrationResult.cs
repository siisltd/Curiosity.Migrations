using System;

namespace Marvin.Migrations
{
    public class MigrationResult
    {
        public bool IsSuccessfully => Error == null;
        
        public MigrationError? Error { get; }
        
        public string ErrorMessage { get; }

        private MigrationResult(MigrationError? error, string errorMessage)
        {
            Error = error;
            ErrorMessage = errorMessage;
        }

        public static MigrationResult SuccessfullyResult()
        {
            return new MigrationResult(null, String.Empty);
        }

        public static MigrationResult FailureResult(MigrationError error, string errorMessage)
        {
            return new MigrationResult(error, errorMessage);
        }
    }

    public enum MigrationError
    {
        Unknown = 0,
        ConnectionError = 1,
        AuthorizationError = 2,
        CreatingDBError = 3,
        CreatingHistoryTable = 4,
        MigratingError = 5,
        MigrationNotFound = 6,
        PolicyError = 7
    }
}