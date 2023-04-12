namespace Curiosity.Migrations;

/// <summary>
/// What should we do if found script file with incorrect naming?
/// </summary>
public enum ScriptIncorrectNamingAction
{
    /// <summary>
    /// Ignore script and process next scripts.
    /// </summary>
    Ignore = 0,

    /// <summary>
    /// Log to warn about incorrect script name and process next scripts.
    /// </summary>
    LogToWarn = 1,

    /// <summary>
    /// Log to error about incorrect script name and process next scripts.
    /// </summary>
    LogToError = 2,

    /// <summary>
    /// Throw exception to abort execution.
    /// </summary>
    ThrowException = 3
}
