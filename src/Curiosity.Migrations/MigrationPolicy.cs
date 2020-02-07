using System;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Migration policy. Can restrict some migrations
    /// </summary>
    [Flags]
    public enum MigrationPolicy
    {
        /// <summary>
        /// All migrations are forbidden
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Only minor migrations are allowed
        /// </summary>
        Minor = 0x1,

        /// <summary>
        /// Only major migrations are allowed
        /// </summary>
        Major = 0x2,

        /// <summary>
        /// All migrations are allowed
        /// </summary>
        All = Minor | Major
    }
}