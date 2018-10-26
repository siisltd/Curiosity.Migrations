namespace Marvin.Migrations.Info
{
    /// <summary>
    /// Database information
    /// </summary>
    public class DbInfo
    {
        /// <summary>
        /// Actual DB version
        /// </summary>
        public DbVersion ActualVersion { get; set; }

        /// <summary>
        /// All migrations for current DB
        /// </summary>
        public DbMigration[] Migrations { get; set; }
    }
}