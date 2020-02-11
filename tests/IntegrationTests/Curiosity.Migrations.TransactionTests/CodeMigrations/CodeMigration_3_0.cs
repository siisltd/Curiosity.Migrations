using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Curiosity.Migrations.TransactionTests.CodeMigrations
{
    public class CodeMigration_3_0 : CodeMigration
    {
        public override DbVersion Version { get; } = new DbVersion(3, 0);

        public override string Comment { get; } = "Migration using multiple EF context with one connection";

        public override async Task UpgradeAsync(DbTransaction transaction)
        {
            var tempContextOptionsBuilder = new DbContextOptionsBuilder<TempContext>();
            tempContextOptionsBuilder.UseNpgsql(DbProvider.Connection);
            
            var anotherTempContextOptionsBuilder = new DbContextOptionsBuilder<AnotherTempContext>();
            anotherTempContextOptionsBuilder.UseNpgsql(DbProvider.Connection);

            using (var tempContext = new TempContext(tempContextOptionsBuilder.Options))
            using (var anotherContext = new AnotherTempContext(anotherTempContextOptionsBuilder.Options))
            {
                tempContext.Database.UseTransaction(transaction);
                anotherContext.Database.UseTransaction(transaction);
                
                var request1 = new BackgroundProcessorRequestEntity
                {
                    CreatedUtc = DateTime.Now,
                    TimeZoneId = "temo",
                    Type = 1,
                    State = 1,
                    UserId = 1,
                    ProjectId = 1,
                    RequestCultureName = "ru"
                };
                var request2 = new BackgroundProcessorRequestEntity
                {
                    CreatedUtc = DateTime.Now,
                    TimeZoneId = "temo",
                    Type = 1,
                    State = 1,
                    UserId = 1,
                    ProjectId = 1,
                    RequestCultureName = "ru"
                };

                tempContext.Requests.Add(request1);
                anotherContext.Requests.Add(request2);

                await tempContext.SaveChangesAsync();
                await anotherContext.SaveChangesAsync();
            }
        }

        public override Task DowngradeAsync(DbTransaction transaction)
        {
            return Task.CompletedTask;
        }

        [Table("background_processor_requests")]
        private class BackgroundProcessorRequestEntity
        {
            [Column("id")]
            public long Id { get; set; }

            [Column("created")]
            public DateTime CreatedUtc { get; set; }

            [Column("time_zone_id")]
            public string TimeZoneId { get; set; }

            [Column("processor_name")]
            public string ProcessorName { get; set; }

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
            public string RequestCultureName { get; set; }

            [Column("log")]
            public string Log { get; set; }

            [Column("params_data_json")]
            public string ParamsDataJson { get; set; }
            
            [Column("result_data_json")]
            public string ResultDataJson { get; set; }
        }

        private class TempContext : DbContext
        {
            public TempContext(DbContextOptions<TempContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<BackgroundProcessorRequestEntity>(entity =>
                {
                    entity.HasKey(e => e.Id);
                });
            }

            public virtual DbSet<BackgroundProcessorRequestEntity> Requests { get; set; }
        }
        
        private class AnotherTempContext : DbContext
        {
            public AnotherTempContext(DbContextOptions<AnotherTempContext> options) : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<BackgroundProcessorRequestEntity>(entity =>
                {
                    entity.HasKey(e => e.Id);
                });
            }

            public virtual DbSet<BackgroundProcessorRequestEntity> Requests { get; set; }
        }
    }


}