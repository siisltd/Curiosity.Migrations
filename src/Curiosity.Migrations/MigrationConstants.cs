namespace Curiosity.Migrations
{
    /// <summary>
    /// Constant values used during migration
    /// </summary>
    public static class MigrationConstants
    {
        /// <summary>
        /// Regex patterns for scanning files
        /// </summary>
        public static readonly string MigrationFileNamePattern = @"(\d+)\.(\d+)(.(down)|.(up))?(-([\w]*))?\.sql$";
    }
}