using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations;

/// <summary>
/// Migration that uses raw sql. 
/// </summary>
public class ScriptMigration : IMigration
{
    protected readonly ILogger? MigrationLogger;
    protected readonly IDbProvider DbProvider;
        
    /// <inheritdoc />
    public DbVersion Version { get; }

    /// <inheritdoc />
    public string? Comment { get; }

    /// <inheritdoc />
    public bool IsTransactionRequired { get; protected set; }

    /// <inheritdoc />
    public bool IsLongRunning { get; protected set; }

    /// <summary>
    /// SQL script to apply migration splitted into batches
    /// </summary>
    public IList<ScriptMigrationBatch> UpScripts { get; }

    public ScriptMigration(
        ILogger? migrationLogger,
        IDbProvider dbProvider,
        DbVersion version,
        ICollection<ScriptMigrationBatch> upScripts,
        string? comment,
        bool isTransactionRequired = true,
        bool isLongRunning = false)
    {
        MigrationLogger = migrationLogger;
        DbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        if (upScripts == null || upScripts.Count == 0) throw new ArgumentException(nameof(upScripts));

        Version = version;
        UpScripts = upScripts.ToArray();
        Comment = comment;
        IsTransactionRequired = isTransactionRequired;
        IsLongRunning = isLongRunning;
    }

    /// <inheritdoc />
    public async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken token = default)
    {
        await RunBatchesAsync(UpScripts, token);
    }

    protected async Task RunBatchesAsync(ICollection<ScriptMigrationBatch> batches, CancellationToken token = default)
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
