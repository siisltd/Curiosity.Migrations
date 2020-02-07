namespace Curiosity.Migrations
{
    /// <summary>
    /// Difference between current DB version and desired migration
    /// </summary>
    public enum DbVersionDifference : byte
    {
        /// <summary>
        /// No difference, DB is correct
        /// </summary>
        NoDifference = 0,

        /// <summary>
        /// Difference in major version number
        /// </summary>
        Minor = 1,

        /// <summary>
        /// Difference in minor version number
        /// </summary>
        Major = 2
    }
}