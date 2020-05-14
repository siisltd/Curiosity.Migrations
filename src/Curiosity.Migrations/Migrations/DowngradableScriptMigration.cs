using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Migration that uses raw sql 
    /// </summary>
    public class DowngradableScriptMigration : ScriptMigration, IDowngradeMigration
    {
        /// <summary>
        /// SQL script to undo migration splitted into batches
        /// </summary>
        public List<ScriptMigrationBatch> DownScripts { get; }
        
        public DowngradableScriptMigration(
            ILogger migrationLogger,
            IDbProvider dbProvider,
            DbVersion version,
            List<ScriptMigrationBatch> upScripts,
            List<ScriptMigrationBatch> downScripts,
            string comment) : base(migrationLogger, dbProvider, version, upScripts, comment)
        {
            DownScripts = downScripts ?? new List<ScriptMigrationBatch>(0);
        }
        
        /// <inheritdoc />
        public async Task DowngradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            await RunBatchesAsync(DownScripts, token);
        }
    }
}