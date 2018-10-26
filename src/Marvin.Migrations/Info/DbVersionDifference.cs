namespace Marvin.Migrations.Info
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
        /// 
        /// </summary>
        Minor = 1,
        
        /// <summary>
        /// 
        /// </summary>
        Major= 2
    }
}