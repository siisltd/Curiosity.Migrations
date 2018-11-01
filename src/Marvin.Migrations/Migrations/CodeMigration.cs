using System;
using System.Threading.Tasks;

namespace Marvin.Migrations.Migrations
{
    public abstract class CodeMigration : IMigration
    {
        public DbVersion Version { get; }
        
        public string Comment { get; }
        
        protected readonly IDbProvider _dbProvider;

        protected CodeMigration(
            IDbProvider dbProvider, 
            DbVersion version, 
            string comment)
        {
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            Version = version;
            Comment = comment;
        }

        public abstract Task UpgradeAsync();

        public abstract Task DowngradeAsync();
    }
}