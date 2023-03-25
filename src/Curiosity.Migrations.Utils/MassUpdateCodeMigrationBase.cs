using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Curiosity.Migrations.Utils;

/// <summary>
/// Base migration for efficient bulk data update in a table.
/// For details on how the migration is performed, read the description of the <see cref="DoMassUpdateAsync"/> method.
/// </summary>
/// <remarks>
/// Disables transactions for migrations. Transactions must be created manually inside this migration.
/// Also sets <see cref="CodeMigration.IsLongRunning"/> option.
/// </remarks>
public abstract class MassUpdateCodeMigrationBase : CodeMigration
{
    /// <summary>
    /// Delay between update iterations.
    /// </summary>
    protected TimeSpan StepDelay { get; }

    /// <inheritdoc cref="MassUpdateCodeMigrationBase"/>
    protected MassUpdateCodeMigrationBase(TimeSpan stepDelay)
    {
        StepDelay = stepDelay;
        IsTransactionRequired = false;
        IsLongRunning = true;
    }

    /// <summary>
    /// Efficiently performs a bulk update of data in a table.
    /// </summary>
    /// <param name="updateQuery">
    /// SQL query to change data. Must contain a CTE with SELECT to get data and UPDATE to change (CTE is for PostgreSQL, use alternatives at your DBMS).
    /// SELECT must have a LIMIT and a <code>WHERE id = @id</code> condition so that it can be iterated.
    /// UPDATE must return the ID of the changed data. An example request can be seen below in the example section.
    /// </param>
    /// <param name="onStepCompleted">
    /// The action that will be called after processing the next migration step.
    /// The total number of rows processed so far is transmitted.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of updated entities.</returns>
    /// <remarks>
    /// The main idea of this update:
    /// <code> - pull out a portion of data for updating via CTE for PostgreSQL and alternatives at another DBMS; </code>
    /// <code>- update them;</code>
    /// <code>- make a short pause so as not to load the database for production environments;</code>
    /// <code>- move on.</code>
    /// We get the data in relatively small portions so that transactions are performed quickly.
    /// If you immediately update all the data in one transaction, the database can lie down. Updating via CTE and small transactions,
    /// we can perform background migration on prod without shutting down the entire system, i.e. effectively.
    /// </remarks>
    /// <example>
    /// This is example for PostgreSQL:
    /// <code>
    /// WITH cte AS (
    ///     SELECT id
    ///     FROM calls 
    ///     WHERE id > @id
    ///     ORDER BY id
    ///     LIMIT 10000)
    /// UPDATE calls c
    ///     SET new_result_code = result_code + 1
    /// FROM cte
    /// WHERE cte.id = c.id
    /// RETURNING cte.id;
    /// </code>
    /// </example>
    protected async Task<long> DoMassUpdateAsync(
        string updateQuery,
        Action<int, long> onStepCompleted,
        CancellationToken cancellationToken = default)
    {
        long currentId = 0;

        // execute update in batches while we can
        var hasData = true;
        long totalProcessed = 0;
        while (hasData)
        {
            IEnumerable<long> processedIds;
            // for each update we make our own transaction so that the changes go quickly
            using (var localTransaction = MigrationConnection.Connection!.BeginTransaction(IsolationLevel.Unspecified))
            {
                var commandParams = new Dictionary<string, object>
                {
                    {"id", currentId},
                };
                var command = new CommandDefinition(updateQuery, commandParams, cancellationToken: cancellationToken);
                processedIds = await MigrationConnection.Connection.QueryAsync<long>(command);

                localTransaction.Commit();
            }

            var idsArray = processedIds.ToArray();
            totalProcessed += idsArray.Length;
            onStepCompleted?.Invoke(idsArray.Length, totalProcessed);

            if (idsArray.Length > 0)
            {
                await Task.Delay(StepDelay, cancellationToken);

                // update currentId
                currentId = idsArray.Max();
            }
            else
            {
                // if there are no more rows, then stop processing
                hasData = false;
            }
        }

        return totalProcessed;
    } 
}
