using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Migration that uses raw sql 
    /// </summary>
    public class ScriptMigration : IMigration
    {
        /// <inheritdoc />
        public DbVersion Version { get; }

        /// <inheritdoc />
        public string Comment { get; }

        /// <summary>
        /// SQL script to apply migration splitted into batches
        /// </summary>
        [NotNull]
        public List<ScriptMigrationBatch> UpScripts { get; }

        /// <summary>
        /// SQL script to undo migration splitted into batches
        /// </summary>
        [NotNull]
        public List<ScriptMigrationBatch> DownScripts { get; }

        private readonly ILogger _migrationLogger;
        private readonly IDbProvider _dbProvider;

        public ScriptMigration(
            ILogger migrationLogger,
            IDbProvider dbProvider,
            DbVersion version,
            List<ScriptMigrationBatch> upScripts,
            List<ScriptMigrationBatch> downScripts,
            string comment)
        {
            _migrationLogger = migrationLogger;
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            if (upScripts == null || upScripts.Count == 0) throw new ArgumentException(nameof(upScripts));

            Version = version;
            UpScripts = upScripts;
            DownScripts = downScripts ?? new List<ScriptMigrationBatch>(0);
            Comment = comment;
        }

        /// <inheritdoc />
        public async Task UpgradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            await RunBatchesAsync(UpScripts, token);
        }

        /// <inheritdoc />
        public async Task DowngradeAsync(DbTransaction transaction, CancellationToken token = default)
        {
            await RunBatchesAsync(DownScripts, token);
        }

        private async Task RunBatchesAsync(List<ScriptMigrationBatch> batches, CancellationToken token = default)
        {
            foreach (var batch in batches.OrderBy(b => b.OrderIndex))
            {
                _migrationLogger?.LogInformation($"Executing migration's batch #{batch.OrderIndex} \"{batch.Name ?? "No name provided"}\"");
                await _dbProvider.ExecuteScriptAsync(batch.Script, token);
            }
        }
    }
}