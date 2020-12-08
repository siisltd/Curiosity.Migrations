using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Curiosity.Migrations.PostgreSQL;
using Xunit;

namespace Curiosity.Migrations.TransactionTests
{
    public class TransactionTests
    {
        [Fact]
        public async Task Migrate_AllScriptOk_NoRollback()
        {
            var config = ConfigProvider.GetConfig();
            var connectionString = String.Format(config.ConnectionStringMask, "test_ok");
            
            
            var builder = new MigratorBuilder();
            builder.UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly());
            builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "ScriptMigrations"));
            builder.UsePostgreSQL(connectionString);

            builder.UseUpgradeMigrationPolicy(MigrationPolicy.Allowed);
            builder.UseDowngradeMigrationPolicy(MigrationPolicy.Allowed);
            builder.SetUpTargetVersion(new DbVersion(3, 0));

            var migrator = builder.Build();

            await migrator.MigrateAsync();
            
            var migrationProvider = new PostgreDbProvider(new PostgreDbProviderOptions(connectionString));
            await migrationProvider.OpenConnectionAsync();
            var actualAppliedMigrations = await migrationProvider.GetAppliedMigrationVersionAsync();
            await migrationProvider.CloseConnectionAsync();
            
            var expectedAppliedMigrations = new HashSet<DbVersion>
            {
                new DbVersion(1,0),
                new DbVersion(2,0),
                new DbVersion(3,0)
            };
            Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
        }
                
        [Fact]
        public async Task Migrate_AllScriptOk_SwitchedOffTransaction()
        {
            var config = ConfigProvider.GetConfig();
            var connectionString = String.Format(config.ConnectionStringMask, "test_without_transactions");
            
            
            var builder = new MigratorBuilder();
            builder.UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly());
            builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "ScriptMigrations"));
            builder.UsePostgreSQL(connectionString);

            builder.UseUpgradeMigrationPolicy(MigrationPolicy.Allowed);
            builder.UseDowngradeMigrationPolicy(MigrationPolicy.Allowed);
            builder.SetUpTargetVersion(new DbVersion(5, 0));

            var migrator = builder.Build();

            await migrator.MigrateAsync();
            
            var migrationProvider = new PostgreDbProvider(new PostgreDbProviderOptions(connectionString));
            await migrationProvider.OpenConnectionAsync();
            var actualAppliedMigrations = await migrationProvider.GetAppliedMigrationVersionAsync();
            await migrationProvider.CloseConnectionAsync();
            
            var expectedAppliedMigrations = new HashSet<DbVersion>
            {
                new DbVersion(1,0),
                new DbVersion(2,0),
                new DbVersion(3,0),
                new DbVersion(4,0),
                new DbVersion(5,0)
            };
            Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
        }
        
        [Fact]
        public async Task Migrate_AllScriptOk_Rollback()
        {
            var config = ConfigProvider.GetConfig();
            var connectionString = String.Format(config.ConnectionStringMask, "test_rollback");
            
            
            var builder = new MigratorBuilder();
            builder.UseCodeMigrations().FromAssembly(Assembly.GetExecutingAssembly());
            builder.UseScriptMigrations().FromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "ScriptMigrations"));
            builder.UsePostgreSQL(connectionString);

            builder.UseUpgradeMigrationPolicy(MigrationPolicy.Allowed);
            builder.UseDowngradeMigrationPolicy(MigrationPolicy.Allowed);
            builder.SetUpTargetVersion(new DbVersion(6, 0));

            var migrator = builder.Build();

            try
            {
                await migrator.MigrateAsync();
                
                // last migration is incorrect, can not go here
                Assert.False(true);
            }
            catch
            {
                // ignored
            }

            var migrationProvider = new PostgreDbProvider(new PostgreDbProviderOptions(connectionString));
            await migrationProvider.OpenConnectionAsync();
            var actualAppliedMigrations = await migrationProvider.GetAppliedMigrationVersionAsync();
            await migrationProvider.CloseConnectionAsync();
            
            var expectedAppliedMigrations = new HashSet<DbVersion>
            {
                new DbVersion(1,0),
                new DbVersion(2,0),
                new DbVersion(3,0),
                new DbVersion(4,0),
                new DbVersion(5,0)
            };
            
            Assert.Equal(expectedAppliedMigrations, actualAppliedMigrations);
        }
    }
}