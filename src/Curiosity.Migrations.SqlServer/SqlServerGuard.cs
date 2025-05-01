using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Curiosity.Migrations;

namespace Curiosity.Migrations.SqlServer;

/// <summary>
/// Guard to check input data on correctness for SqlServer provider.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class SqlServerGuard
{
    /// <summary>
    /// Asserts that connection string is valid.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static void AssertConnectionString(string connectionString, string? paramName = null)
    {
        Guard.AssertNotEmpty(connectionString, paramName ?? nameof(connectionString));
    }

    /// <summary>
    /// Asserts that connection is valid and open.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AssertConnection(SqlConnection? connection, string? paramName = null)
    {
        if (connection == null)
            throw new ArgumentNullException(paramName ?? "connection");
            
        if (connection.State != ConnectionState.Open)
            throw new InvalidOperationException($"Connection must be opened (current state: {connection.State})");
    }

    /// <summary>
    /// Asserts that table name is valid.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void AssertTableName(string tableName, string? paramName = null)
    {
        Guard.AssertNotEmpty(tableName, paramName ?? nameof(tableName));
    }
} 