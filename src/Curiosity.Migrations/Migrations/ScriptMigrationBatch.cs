using System;

namespace Curiosity.Migrations;

/// <summary>
/// Batch of single migration
/// </summary>
/// <remarks>
/// Script migration is split into batches by text "--BATCH:"
/// </remarks>
public class ScriptMigrationBatch
{
    /// <summary>
    /// Batch order in a migration.
    /// </summary>
    public int OrderIndex { get; }
            
    /// <summary>
    /// Batch name (shown at logs, name is a text after --BATCH:).
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// SQL to execute.
    /// </summary>
    public string Script
    {
        get => _script;
        internal set
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(Script));

            _script = value;
        }
    }
    private string _script;

    /// <inheritdoc cref="ScriptMigrationBatch"/>
    public ScriptMigrationBatch(
        int orderIndex,
        string? name,
        string script)
    {
        Guard.AssertNotEmpty(script, nameof(script));
        if (orderIndex < 0) throw new ArgumentOutOfRangeException(nameof(orderIndex));
            
        OrderIndex = orderIndex;
        Name = name;
        _script = script;
    }
}
