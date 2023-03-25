using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations;

/// <summary>
/// Abstract class for migration with custom logic written in C#
/// </summary>
/// <remarks>
/// Code migrations allows to make complex migrations with complex logic, which are extremely difficult to perform on a clean SQL queries.
/// </remarks>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract class CodeMigration : IMigration
{
    /// <inheritdoc />
    public abstract DbVersion Version { get; }

    /// <inheritdoc />
    public abstract string Comment { get; }

    /// <inheritdoc />
    public bool IsTransactionRequired { get; protected set; } = true;

    /// <inheritdoc />
    public bool IsLongRunning { get; protected set; } = false;

    /// <summary>
    /// Provides access to underlying database.
    /// </summary>
    protected IMigrationConnection MigrationConnection { get; private set; } = null!;

    /// <summary>
    /// User defined variables.
    /// </summary>
    protected IReadOnlyDictionary<string, string> Variables { get; private set; } = null!;

    /// <summary>
    /// Logger for migration.
    /// </summary>
    /// <remarks>
    /// Can be <see langword="null"/> if logger wasn't set up
    /// </remarks>
    protected ILogger? Logger { get; private set; }

    /// <summary>
    /// Initializes the migration. 
    /// </summary>
    /// <param name="migrationConnection">Provider for DB access</param>
    /// <param name="variables">Variables for migrations</param>
    /// <param name="migrationLogger">Logger for migration</param>
    internal void Init(
        IMigrationConnection migrationConnection,
        IReadOnlyDictionary<string, string> variables,
        ILogger? migrationLogger)
    {
        Guard.AssertNotNull(migrationConnection, nameof(migrationConnection));
        Guard.AssertNotNull(variables, nameof(variables));

        MigrationConnection = migrationConnection ?? throw new ArgumentNullException(nameof(migrationConnection));
        Variables = variables ?? throw new ArgumentNullException(nameof(variables));
        Logger = migrationLogger;
    }

    /// <inheritdoc />
    public abstract Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken token = default);
}
