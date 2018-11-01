using System;
using System.Threading.Tasks;

namespace Marvin.Migrations.Info
{
    public interface IMigration
    {
        /// <summary>
        /// Migration version
        /// </summary>
        DbVersion Version { get; }
        
        string Comment { get; }

        Task UpgradeAsync();

        Task DowngradeAsync();
    }

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
            //throw new System.NotImplementedException();
            return Task.CompletedTask;
        }

        public Task DowngradeAsync()
        {
            //throw new System.NotImplementedException();
            return Task.CompletedTask;
        }
    }

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