namespace Curiosity.Migrations
{
    /// <summary>
    /// Current database state
    /// </summary>
    public enum DbState
    {
        /// <summary>
        /// Unknown. 
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Database or history table does not created
        /// </summary>
        NotCreated = 1,

        /// <summary>
        /// Version of database is less than version of migrations
        /// </summary>
        Outdated = 2,

        /// <summary>
        /// Version of database is newer than version of migrations
        /// </summary>
        Newer = 3,

        /// <summary>
        /// Database is actual
        /// </summary>
        Ok = 4
    }
}