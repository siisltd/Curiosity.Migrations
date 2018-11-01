using System;
using System.Threading.Tasks;

namespace Marvin.Migrations.Migrations
{
    public abstract class CodeMigration : IMigration
    {
        public abstract DbVersion Version { get; }
        
        public abstract string Comment { get; }
        
        protected readonly IDbProvider _dbProvider;

        protected CodeMigration(
            IDbProvider dbProvider)
        {
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        public abstract Task UpgradeAsync();

        public abstract Task DowngradeAsync();
    }
}