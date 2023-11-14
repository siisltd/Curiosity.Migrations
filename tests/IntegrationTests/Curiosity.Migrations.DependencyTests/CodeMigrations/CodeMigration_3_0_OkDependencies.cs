using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Curiosity.Migrations.DependencyTests.CodeMigrations;

public class CodeMigration_3_0_OkDependencies : CodeMigration
{
    /// <inheritdoc />
    public override MigrationVersion Version { get; } = new(3);

    /// <inheritdoc />
    public override string Comment { get; } = "Migration using multiple EF context with one connection";

    public CodeMigration_3_0_OkDependencies()
    {
        Dependencies = new List<MigrationVersion>() { new(1,0), new(2,0) };
    }
    
    /// <inheritdoc />
    public override async Task UpgradeAsync(DbTransaction? transaction = null, CancellationToken cancellationToken = default)
    {
        await MigrationConnection.ExecuteNonQuerySqlAsync("select 1;", null, cancellationToken);
    }
}
