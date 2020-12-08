namespace Curiosity.Migrations
{
    /// <summary>
    /// Constant values used during migration
    /// </summary>
    public static class MigrationConstants
    {
        /// <summary>
        /// Regex pattern for parsing version
        /// </summary>
        public static readonly string VersionPattern = @"([\d|-]+)(\.(\d+))*";
        
        /// <summary>
        /// Regex pattern for scanning files
        /// </summary>
        public static readonly string MigrationFileNamePattern = $@"({VersionPattern})(.(down)|.(up))?(-([\w]*))?\.sql$";
    }
}