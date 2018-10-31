using System.Collections.Generic;
using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;

namespace Marvin.Migrations
{
    public interface IMigrationsProvider
    {
        ICollection<IMigration> GetMigrations(IDbProvider dbProvider);
    }
}