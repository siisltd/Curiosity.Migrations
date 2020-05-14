using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Migration that uses raw sql 
    /// </summary>
    public class ScriptMigration : IMigration
    {
        protected readonly ILogger MigrationLogger;
        protected readonly IDbProvider DbProvider;
        
        /// <inheritdoc />
        public DbVersion Version { get; }

        /// <inheritdoc />
        public string Comment { get; }

        /// <summary>
        /// SQL script to apply migration splitted into batches
        /// </summary>
        public List<ScriptMigrationBatch> UpScripts { get; }

        public ScriptMigration(
            ILogger migrationLogger,
            IDbProvider dbProvider,
            DbVersion version,
            List<ScriptMigrationBatch> upScripts,
            string comment)
        {
            MigrationLogger = migrationLogger;
            DbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            if (upScripts == null || upScripts.Count == 0) throw new ArgumentException(nameof(upScripts));

            Version = version;
            UpScripts = upScripts;
            Comment = comment;
        }

        /// <inheritdoc />
        public async Task UpgradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            await RunBatchesAsync(UpScripts, token);
        }

        protected async Task RunBatchesAsync(List<ScriptMigrationBatch> batches, CancellationToken token = default)
        {
            var needLogBatches = batches.Count > 1;
            foreach (var batch in batches.OrderBy(b => b.OrderIndex))
            {
                if (needLogBatches)
                {
                    MigrationLogger?.LogInformation(
                        $"Executing migration's batch #{batch.OrderIndex} \"{batch.Name ?? "No name provided"}\"");
                }
                await DbProvider.ExecuteScriptAsync(batch.Script, token);
            }
        }
    }
}