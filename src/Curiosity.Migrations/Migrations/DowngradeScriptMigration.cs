using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations;

/// <summary>
/// Migration that uses raw sql to downgrade a database 
/// </summary>
public class DowngradeScriptMigration : ScriptMigration, IDowngradeMigration
{
    /// <summary>
    /// SQL script to undo migration split into batches
    /// </summary>
    public IReadOnlyList<ScriptMigrationBatch> DownScripts { get; }

    /// <inheritdoc cref="DowngradeScriptMigration"/>
    public DowngradeScriptMigration(
        ILogger? migrationLogger,
        IMigrationConnection migrationConnection,
        MigrationVersion version,
        IReadOnlyList<ScriptMigrationBatch> upScripts,
        IReadOnlyList<ScriptMigrationBatch>? downScripts,
        string? comment,
        bool isTransactionRequired = true,
        bool isLongRunning = false)
        : base(
            migrationLogger,
            migrationConnection,
            version,
            upScripts,
            comment,
            isTransactionRequired,
            isLongRunning)
    {
        DownScripts = downScripts?.ToArray() ?? Array.Empty<ScriptMigrationBatch>();
    }

    /// <inheritdoc />
    public Task DowngradeAsync(DbTransaction? transaction = null, CancellationToken token = default)
    {
        return RunBatchesAsync(DownScripts, token);
    }
}
