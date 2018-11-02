namespace Marvin.Migrations.PostgreSQL
{
    /// <summary>
    /// Extension to adding Postgre migrations to <see cref="MigratorBuilder"/>
    /// </summary>
    public static class MigratorBuilderExtensions
    {
        /// <summary>
        /// Use provider to make migration on Postgre database
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionString">Connection string to Postgre</param>
        /// <param name="targetVersion">Target migration version</param>
        /// <returns></returns>
        public static MigratorBuilder UsePostgreSQL(
            this MigratorBuilder builder,
            string connectionString,
            string migrationTableHistoryName= null,
            string databaseEncoding = null,
            string lcCollate = null,
            string lcCtype = null, 
            int? connectionLimit = null)
        {
            var options = new PostgreDbProviderOptions(
                connectionString,
                migrationTableHistoryName,
                databaseEncoding,
                lcCollate,
                lcCtype,
                connectionLimit);
            builder.UserDbProviderFactory(new PostgreDbProviderFactory(options));
            return builder;
        }
    }
}