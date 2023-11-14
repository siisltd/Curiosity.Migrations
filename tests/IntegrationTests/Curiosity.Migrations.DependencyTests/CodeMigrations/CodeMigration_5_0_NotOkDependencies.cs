using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Curiosity.Migrations.DependencyTests.CodeMigrations;

public class CodeMigration_5_0_NotOkDependencies : CodeMigration
{
    /// <inheritdoc />
    public override MigrationVersion Version { get; } = new(5);

    /// <inheritdoc />
    public override string Comment => "Migrations with switched off transactions";

    public CodeMigration_5_0_NotOkDependencies()
    {
        Dependencies = new List<MigrationVersion>() { new(1,0), new(6,0) };
        IsLongRunning = true;
    }

    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync("select 1;", null, cancellationToken);
    }
}
