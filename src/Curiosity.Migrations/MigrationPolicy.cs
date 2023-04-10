using System;

namespace Curiosity.Migrations;

/// <summary>
/// Migration policy. Specifies what kind of migrations are allowed to apply.
/// </summary>
[Flags]
public enum MigrationPolicy
{
    /// <summary>
    /// All migrations are forbidden.
    /// </summary>
    AllForbidden = 0x000000,

    /// <summary>
    /// Allowed to run short running migrations.
    /// </summary>
    ShortRunningAllowed = 0x000001,

    /// <summary>
    /// Allowed to run long running migrations.
    /// </summary>
    LongRunningAllowed = 0x000002,

    /// <summary>
    /// All migrations are allowed.
    /// </summary>
    AllAllowed = 0xFFFFFF
}
