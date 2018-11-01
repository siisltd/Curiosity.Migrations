using System.Collections.Generic;
using Marvin.Migrations.Info;

namespace Marvin.Migrations.MigrationProviders
{
    public interface IMigrationsProvider
    {
        ICollection<IMigration> GetMigrations(IDbProvider dbProvider);
    }
}