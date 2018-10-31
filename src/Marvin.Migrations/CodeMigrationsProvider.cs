using System.Collections.Generic;
using Marvin.Migrations.DatabaseProviders;
using Marvin.Migrations.Info;

namespace Marvin.Migrations
{
    public class CodeMigrationsProvider : IMigrationsProvider
    {
        public ICollection<IMigration> GetMigrations(IDbProvider dbProvider)
        {
            throw new System.NotImplementedException();
        }
    }
}