using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
    // ReSharper disable MemberCanBePrivate.Global
    protected readonly ILogger? MigrationLogger;
    protected readonly IMigrationConnection MigrationConnection;
    // ReSharper restore MemberCanBePrivate.Global
        
    /// <inheritdoc />
    public MigrationVersion Version { get; }

    /// <inheritdoc />
    public string? Comment { get; }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public bool IsTransactionRequired { get; protected set; }

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    public bool IsLongRunning { get; protected set; }

    /// <summary>
    /// SQL script to apply migration split into batches.
    /// </summary>
    public IReadOnlyList<ScriptMigrationBatch> UpScripts { get; }

    /// <inheritdoc cref="ScriptMigration"/>
    public ScriptMigration(
        ILogger? migrationLogger,
        IMigrationConnection migrationConnection,
        MigrationVersion version,
        IReadOnlyList<ScriptMigrationBatch> upScripts,
        string? comment,
        bool isTransactionRequired = true,
        bool isLongRunning = false)
    {
        Guard.AssertNotNull(migrationConnection, nameof(migrationConnection));
        Guard.AssertNotEmpty(upScripts, nameof(upScripts));

        MigrationConnection = migrationConnection;
        MigrationLogger = migrationLogger;

        Version = version;
        UpScripts = upScripts.ToArray();
        Comment = comment;
        IsTransactionRequired = isTransactionRequired;
        IsLongRunning = isLongRunning;
    }

    /// <inheritdoc />
    public async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        await RunBatchesAsync(UpScripts, cancellationToken);
    }

    /// <summary>
    /// Executes migration's batches.
    /// </summary>
    protected async Task RunBatchesAsync(
        IReadOnlyList<ScriptMigrationBatch> batches,
        CancellationToken token = default)
    {
        Guard.AssertNotNull(batches, nameof(batches));

        var needLogBatches = batches.Count > 1;
        foreach (var batch in batches.OrderBy(b => b.OrderIndex))
        {
            if (needLogBatches)
            {
                MigrationLogger?.LogInformation(
                    $"Executing migration's batch #{batch.OrderIndex} \"{batch.Name ?? "No name provided"}\"");
            }
            await MigrationConnection.ExecuteNonQuerySqlAsync(
                batch.Script,
                null,
                token);
        }
    }
}
