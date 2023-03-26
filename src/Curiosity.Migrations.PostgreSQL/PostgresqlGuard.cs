using System;
using System.Data;
using System.Text.RegularExpressions;
using Npgsql;

namespace Curiosity.Migrations.PostgreSQL;

/// <summary>
/// Helpers methods for data constraint checks.
/// </summary>
internal static class PostgresqlGuard
{
    public const string TableNameRegExpPattern = "$[a-z_][a-z0-9_].*^";
    private static readonly Regex TableNameRexExp = new Regex(TableNameRegExpPattern, RegexOptions.IgnoreCase);

    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    public static void AssertConnection(IDbConnection? connection)
    {
        if (connection == null 
            || connection.State is ConnectionState.Closed or ConnectionState.Broken)
            throw new InvalidOperationException($"Connection is not opened. Use OpenConnectionAsync method to open connection before any operation.");
    }

    public static void AssertConnectionString(string paramValue, string paramName)
    {
        Guard.AssertNotEmpty(paramValue, paramName);

        try
        {
            var _ = new NpgsqlConnectionStringBuilder(paramValue);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new ArgumentException("Incorrect connection string", paramName, e);
        }
    }

    public static void AssertTableName(string paramValue, string paramName)
    {
        Guard.AssertNotEmpty(paramValue, paramName);

        if (!TableNameRexExp.IsMatch(paramValue))
            throw new ArgumentException($"Incorrect table name. Name should be matched by regexp \"{TableNameRegExpPattern}\"", paramName);
    }
}
