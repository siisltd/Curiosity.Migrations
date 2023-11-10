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
        var tempContextOptionsBuilder = new DbContextOptionsBuilder<TempContext>();
        tempContextOptionsBuilder.UseNpgsql(MigrationConnection.Connection!);

        var anotherTempContextOptionsBuilder = new DbContextOptionsBuilder<AnotherTempContext>();
        anotherTempContextOptionsBuilder.UseNpgsql(MigrationConnection.Connection!);

        await using (var tempContext = new TempContext(tempContextOptionsBuilder.Options))
        await using (var anotherContext = new AnotherTempContext(anotherTempContextOptionsBuilder.Options))
        {
            // tempContext.Database.UseTransaction(transaction);
            // anotherContext.Database.UseTransaction(transaction);

            var request1 = new BackgroundProcessorRequestEntity
            {
                CreatedUtc = DateTime.UtcNow,
                TimeZoneId = "temo",
                Type = 1,
                State = 1,
                UserId = 1,
                ProjectId = 1,
                RequestCultureName = "ru"
            };
            var request2 = new BackgroundProcessorRequestEntity
            {
                CreatedUtc = DateTime.UtcNow,
                TimeZoneId = "temo",
                Type = 1,
                State = 1,
                UserId = 1,
                ProjectId = 1,
                RequestCultureName = "ru"
            };

            tempContext.Requests.Add(request1);
            anotherContext.Requests.Add(request2);

            await tempContext.SaveChangesAsync(cancellationToken);
            await anotherContext.SaveChangesAsync(cancellationToken);
        }
    }

    [Table("background_processor_requests")]
    private class BackgroundProcessorRequestEntity
    {
        [Column("id")]
        public long Id { get; set; }

        [Column("created")]
        public DateTime CreatedUtc { get; set; }

        [Column("time_zone_id")]
        public string TimeZoneId { get; set; } = null!;

        [Column("processor_name")]
        public string ProcessorName { get; set; } = null!;

        [Column("type")]
        public int Type { get; set; }

        [Column("state")]
        public int State { get; set; }

        [Column("start_processing")]
        public DateTime? StartProcessingUtc { get; set; }

        [Column("finish_processing")]
        public DateTime? FinishProcessingUtc { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [Column("project_id")]
        public long? ProjectId { get; set; }

        [Column("culture")]
        public string RequestCultureName { get; set; } = null!;

        [Column("log")]
        public string Log { get; set; } = null!;

        [Column("params_data_json")]
        public string ParamsDataJson { get; set; } = null!;

        [Column("result_data_json")]
        public string ResultDataJson { get; set; } = null!;
    }

    private class TempContext : DbContext
    {
        public TempContext(DbContextOptions<TempContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BackgroundProcessorRequestEntity>(entity => { entity.HasKey(e => e.Id); });
        }

        public virtual DbSet<BackgroundProcessorRequestEntity> Requests { get; set; } = null!;
    }

    private class AnotherTempContext : DbContext
    {
        public AnotherTempContext(DbContextOptions<AnotherTempContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BackgroundProcessorRequestEntity>(entity => { entity.HasKey(e => e.Id); });
        }

        public virtual DbSet<BackgroundProcessorRequestEntity> Requests { get; set; } = null!;
    }
}
