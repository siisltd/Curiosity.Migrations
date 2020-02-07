namespace Curiosity.Migrations.PostgreSQL
{
    /// <summary>
    /// Extension to adding Postgre migrations to <see cref="MigratorBuilder"/>
    /// </summary>
    public static class MigratorBuilderExtensions
    {
        /// <summary>
        /// Use provider to make migration on Postgre database. 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="connectionString">Connection string to Postgre</param>
        /// <param name="migrationTableHistoryName">Migration history table name. If param <see langword="null"/> default value from DB will be used </param>
        /// <param name="databaseEncoding">Text presentation of database encoding for Postgre. If <see langword="null"/> default value from DB will be used </param>
        /// <param name="lcCollate"> String sort order for Postgre. If param <see langword="null"/> default value from DB will be used </param>
        /// <param name="lcCtype">Character classification for Postgre. If param <see langword="null"/> default value from DB will be used</param>
        /// <param name="connectionLimit">Limit of connections to Postgre. If param <see langword="null"/> default value from DB will be used </param>
        /// <param name="template">The name of the template from which to create the new database. If param <see langword="null"/> default value from DB will be used</param>
        /// <param name="tableSpace">The name of the tablespace that will be associated with the new database. If param <see langword="null"/> default value from DB will be used </param>
        /// <remarks>
        /// For detailed params description look at <see cref="PostgreDbProviderOptions"/>
        /// </remarks>
        // ReSharper disable once InconsistentNaming
        public static MigratorBuilder UsePostgreSQL(
            this MigratorBuilder builder,
            string connectionString,
            string migrationTableHistoryName = null,
            string databaseEncoding = null,
            string lcCollate = null,
            string lcCtype = null,
            int? connectionLimit = null,
            string template = null,
            string tableSpace = null)
        {
            var options = new PostgreDbProviderOptions(
                connectionString,
                migrationTableHistoryName,
                databaseEncoding,
                lcCollate,
                lcCtype,
                connectionLimit,
                template,
                tableSpace);
            builder.UserDbProviderFactory(new PostgreDbProviderFactory(options));
            return builder;
        }
    }
}