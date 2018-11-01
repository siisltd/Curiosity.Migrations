using System;

namespace Marvin.Migrations.Exceptions
{
    public class MigrationException : Exception
    {
        public MigrationError Error { get; }
        
        public MigrationException(MigrationError error)
        {
            Error = error;
        }

        public MigrationException(MigrationError error, string message) : base(message)
        {
            Error = error;
        }

        public MigrationException(MigrationError error, string message, Exception inner) : base(message, inner)
        {
            Error = error;
        }
    }
}