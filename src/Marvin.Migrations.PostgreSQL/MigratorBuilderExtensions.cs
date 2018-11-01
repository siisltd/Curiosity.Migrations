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
            DbVersion? targetVersion = null)
        {
            if (targetVersion.HasValue)
            {
                builder.SetUpTargetVersion(targetVersion.Value);
            }
            builder.UserDbProvider(new PostgreDbProvider(connectionString));
            return builder;
        }
    }
}