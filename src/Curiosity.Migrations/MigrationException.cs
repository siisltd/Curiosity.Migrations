using System;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Exception occured during migration
    /// </summary>
    public class MigrationException : Exception
    {
        /// <summary>
        /// Code of migration error
        /// </summary>
        public MigrationError Error { get; }

        /// <inheritdoc />
        public MigrationException(MigrationError error)
        {
            Error = error;
        }

        /// <inheritdoc />
        public MigrationException(MigrationError error, string message) : base(message)
        {
            Error = error;
        }

        /// <inheritdoc />
        public MigrationException(MigrationError error, string message, Exception inner) : base(message, inner)
        {
            Error = error;
        }
    }
}