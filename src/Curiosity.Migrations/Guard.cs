using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Curiosity.Migrations;

/// <summary>
/// Helpers methods for data constraint checks.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Checks, that parameter is not null.
    /// </summary>
    /// <param name="parameter">Parameter's name.</param>
    /// <param name="parameterName">Parameter's value.</param>
    /// <typeparam name="T">Parameter's type.</typeparam>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertNotNull<T>(T? parameter, string parameterName) where T : class
    {
        if (parameter == null)
            throw new ArgumentNullException(parameterName);
    }

    /// <summary>
    /// Checks, that string is not empty.
    /// </summary>
    /// <param name="parameter">Parameter's name.</param>
    /// <param name="parameterName">Parameter's value.</param>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertNotEmpty(string? parameter, string parameterName)
    {
        if (String.IsNullOrWhiteSpace(parameter))
            throw new ArgumentNullException(parameterName);
    }
    
    /// <summary>
    /// Checks, that string is not empty.
    /// </summary>
    /// <param name="parameter">Parameter's name.</param>
    /// <param name="parameterName">Parameter's value.</param>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AssertNotEmpty<T>(ICollection<T>? parameter, string parameterName)
    {
        AssertNotNull(parameter, parameterName);

        if (parameter!.Count == 0)
            throw new ArgumentException($"{parameterName} can't be empty", parameterName);
    }
}
