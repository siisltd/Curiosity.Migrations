using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Curiosity.Migrations.SqlServer;

/// <summary>
/// Helper class to try executing actions for migrations with catching errors, logging errors
/// and wrapping errors to <see cref="MigrationException"/>.
/// </summary>
internal class MigrationActionHelper
{
    private readonly string _databaseName;

    /// <inheritdoc cref="MigrationActionHelper"/>
    /// <param name="databaseName">Name of connected database.</param>
    internal MigrationActionHelper(string databaseName)
    {
        Guard.AssertNotEmpty(databaseName, nameof(databaseName));

        _databaseName = databaseName;
    }

    /// <summary>
    /// Tries to execute async function with error handling.
    /// </summary>
    /// <param name="func">Func to execute.</param>
    /// <param name="errorCode">Type of error if occurred.</param>
    /// <param name="errorMessage">Error message if error occurred.</param>
    /// <typeparam name="T">Result type of func.</typeparam>
    /// <returns>Result of <see cref="func"/>.</returns>
    /// <exception cref="MigrationException">If <see cref="func"/> throw exception.</exception>
    internal async Task<T> TryExecuteAsync<T>(
        Func<CancellationToken, Task<T>> func,
        MigrationErrorCode errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await func(cancellationToken);
        }
        catch (MigrationException)
        {
            throw;
        }
        catch (SqlException sqlEx)
        {
            throw CreateMigrationExceptionFromSqlException(sqlEx, errorCode, errorMessage);
        }
        catch (Exception e)
        {
            throw new MigrationException(
                errorCode,
                $"{errorMessage} Database: {_databaseName}",
                e);
        }
    }

    /// <summary>
    /// Tries to execute async function with error handling using cancellation.
    /// </summary>
    /// <param name="func">Func to execute.</param>
    /// <param name="errorCode">Type of error if occurred.</param>
    /// <param name="errorMessage">Error message if error occurred.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of <see cref="func"/>.</returns>
    /// <exception cref="MigrationException">If <see cref="func"/> throw exception.</exception>
    internal async Task TryExecuteAsync(
        Func<CancellationToken, Task> func,
        MigrationErrorCode errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await func(cancellationToken);
        }
        catch (MigrationException)
        {
            throw;
        }
        catch (SqlException sqlEx)
        {
            throw CreateMigrationExceptionFromSqlException(sqlEx, errorCode, errorMessage);
        }
        catch (Exception e)
        {
            throw new MigrationException(
                errorCode,
                $"{errorMessage} Database: {_databaseName}",
                e);
        }
    }

    /// <summary>
    /// Creates a MigrationException from a SqlException with an appropriate error code.
    /// </summary>
    private MigrationException CreateMigrationExceptionFromSqlException(
        SqlException sqlEx, 
        MigrationErrorCode defaultErrorCode, 
        string errorMessage)
    {
        // Map SQL Server error codes to appropriate migration error codes
        var errorCode = GetErrorCodeFromSqlException(sqlEx, defaultErrorCode);
        
        // Add SQL error information to the error message
        var detailedErrorMessage = $"{errorMessage}. SQL Error: {sqlEx.Number}, State: {sqlEx.State}, Message: {sqlEx.Message}. Database: {_databaseName}";
        
        return new MigrationException(
            errorCode,
            detailedErrorMessage,
            sqlEx);
    }

    /// <summary>
    /// Maps SQL Server error codes to MigrationErrorCode
    /// </summary>
    private MigrationErrorCode GetErrorCodeFromSqlException(SqlException sqlEx, MigrationErrorCode defaultErrorCode)
    {
        // Common SQL Server error codes
        return sqlEx.Number switch
        {
            // Login failures
            18456 => MigrationErrorCode.AuthorizationError,
            
            // Database doesn't exist
            4060 => MigrationErrorCode.CreatingDbError,
            
            // Connection issues
            -2 or 53 or 10060 => MigrationErrorCode.ConnectionError,
            
            // Default fallback to provided default error code or Unknown if no default provided
            _ => defaultErrorCode != MigrationErrorCode.Unknown 
                ? defaultErrorCode 
                : MigrationErrorCode.MigratingError
        };
    }
} 