namespace Curiosity.Migrations
{
    /// <summary>
    /// Migration policy. Can restrict some migrations
    /// </summary>
    public enum MigrationPolicy
    {
        /// <summary>
        /// All migrations are forbidden
        /// </summary>
        Forbidden = 0x0,

        /// <summary>
        /// All migrations are allowed
        /// </summary>
        Allowed = 0x1
    }
}
