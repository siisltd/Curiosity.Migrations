using System;
using System.Data;

namespace Curiosity.Migrations.PostgreSQL;

/// <summary>
/// Helpers methods for data constraint checks.
/// </summary>
internal static class PostgresqlGuard
{
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    public static void AssertConnection(IDbConnection? connection)
    {
        if (connection == null 
            || connection.State is ConnectionState.Closed or ConnectionState.Broken)
            throw new InvalidOperationException($"Connection is not opened. Use OpenConnectionAsync method to open connection before any operation.");
    }

    //todo validate params
}
