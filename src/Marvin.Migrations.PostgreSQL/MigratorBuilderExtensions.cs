using System.Runtime.CompilerServices;
using Marvin.Migrations.Info;

namespace Marvin.Migrations.PostgreSQL
{
    public static class MigratorBuilderExtensions
    {
        public static MigratorBuilder UsePostgreSQL(
            this MigratorBuilder builder,
            string connectionString,
            DbVersion? targetVersion = null)
        {
            builder.UserDbProvider(new PostgreDbProvider(connectionString));
            return builder;
        }
    }
}