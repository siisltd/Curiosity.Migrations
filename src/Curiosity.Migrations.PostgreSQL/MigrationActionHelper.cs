using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Curiosity.Migrations.PostgreSQL;

/// <summary>
/// Helper to execute action with a database and process thrown exceptions.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal class MigrationActionHelper
{
    private readonly string _databaseName;

    /// <inheritdoc cref="MigrationActionHelper"/>
    public MigrationActionHelper(string databaseName)
    {
        _databaseName = databaseName;
    }

    /// <summary>
    /// Tries to execute passed function catching common postgres exceptions and return result.
    /// All caught exceptions will be processed and re-thrown as <see cref="MigrationException"/>.
    /// </summary>
    /// <param name="action">Code that should be executed and that can cause postgres exceptions.</param>
    /// <param name="errorCodeType">Type for exception that will be thrown if unknown exception occurs.</param>
    /// <param name="errorMessage">Message for exception that will be thrown if unknown exception occurs.</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T">Type of returned result.</typeparam>
    /// <returns>Result of invoking passed function.</returns>
    /// <exception cref="InvalidOperationException">In case when IOE get caught - it will be rethrown.</exception>
    /// <exception cref="MigrationException">Any exception except IOE will be rethrown as MigrationException.</exception>
    public async Task<T> TryExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        MigrationErrorCode errorCodeType,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotNull(action, nameof(action));
        Guard.AssertNotEmpty(errorMessage, nameof(errorMessage));

        try
        {
            return await action.Invoke(cancellationToken);
        }
        catch (MigrationException)
        {
            throw;
        }
        catch (PostgresException e)
        {
            throw CreateMigrationExceptionFromPostgresException(e, errorCodeType, errorMessage);
        }
        catch (NpgsqlException e)
        {
            throw new MigrationException(MigrationErrorCode.MigratingError, $"Error occured while migrating database \"{_databaseName}\"", e);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new MigrationException(errorCodeType, errorMessage, e);
        }
    }

    /// <inheritdoc cref="TryExecuteAsync{T}"/>
    public async Task TryExecuteAsync(
        Func<CancellationToken, Task> action,
        MigrationErrorCode errorCodeType,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        Guard.AssertNotNull(action, nameof(action));
        Guard.AssertNotEmpty(errorMessage, nameof(errorMessage));

        try
        {
            await action.Invoke(cancellationToken);
        }
        catch (MigrationException)
        {
            throw;
        }
        catch (PostgresException e)
        {
            throw CreateMigrationExceptionFromPostgresException(e, errorCodeType, errorMessage);
        }
        catch (NpgsqlException e)
        {
            throw new MigrationException(MigrationErrorCode.MigratingError, $"Error occured while migrating the database \"{_databaseName}\"", e);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new MigrationException(errorCodeType, errorMessage, e);
        }
    }

    /// <summary>
    /// Creates a MigrationException from a PostgresException with an appropriate error code.
    /// </summary>
    private MigrationException CreateMigrationExceptionFromPostgresException(
        PostgresException e,
        MigrationErrorCode defaultErrorCode,
        string errorMessage)
    {
        if (e.SqlState.StartsWith("08") || e.SqlState is "3D000" or "3F000")
        {
            return new MigrationException(
                MigrationErrorCode.ConnectionError,
                $"Can not connect to database \"{_databaseName}\"",
                e);
        }
        
        if (e.SqlState.StartsWith("28") || e.SqlState is "0P000" or "42501" or "42000")
        {
            return new MigrationException(
                MigrationErrorCode.AuthorizationError,
                $"Invalid authorization specification for \"{_databaseName}\"",
                e);
        }
        
        return new MigrationException(
            defaultErrorCode,
            $"{errorMessage}. PostgreSQL Error: {e.SqlState}, Message: {e.Message}. Database: {_databaseName}",
            e);;
    }
}
