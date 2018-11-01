using System;
using System.Threading.Tasks;

namespace Marvin.Migrations.Migrations
{
    public class ScriptMigration : IMigration
    {
        public DbVersion Version { get; }
        
        public string UpScript { get; }
        
        public string DownScript { get; }
        
        public string Comment { get; }

        private readonly IDbProvider _dbProvider;

        public ScriptMigration(
            IDbProvider dbProvider, 
            DbVersion version, 
            string upScript, 
            string downScript, 
            string comment)
        {
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            if (String.IsNullOrWhiteSpace(upScript)) throw new ArgumentException(nameof(upScript));
            
            Version = version;
            UpScript = upScript;
            DownScript = downScript;
            Comment = comment;
        }

        public Task UpgradeAsync()
        {
            return _dbProvider.ExecuteScriptAsync(UpScript);
        }

        public Task DowngradeAsync()
        {
            if (String.IsNullOrWhiteSpace(DownScript)) return Task.CompletedTask;
            
            return _dbProvider.ExecuteScriptAsync(DownScript);
        }
    }
}