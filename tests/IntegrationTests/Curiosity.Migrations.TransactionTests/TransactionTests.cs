using System;
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

            builder.UseUpgradeMigrationPolicy(MigrationPolicy.All);
            builder.UseDowngradeMigrationPolicy(MigrationPolicy.All);
            builder.SetUpTargetVersion(new DbVersion(3, 0));

            var migrator = builder.Build();

            await migrator.MigrateAsync();
            
            var migrationProvider = new PostgreDbProvider(new PostgreDbProviderOptions(connectionString));
            await migrationProvider.OpenConnectionAsync();
            var currentDbVersion = await migrationProvider.GetDbVersionSafeAsync();
            await migrationProvider.CloseConnectionAsync();
            
            Assert.Equal(new DbVersion(3,0), currentDbVersion);
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

            builder.UseUpgradeMigrationPolicy(MigrationPolicy.All);
            builder.UseDowngradeMigrationPolicy(MigrationPolicy.All);
            builder.SetUpTargetVersion(new DbVersion(4, 0));

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
            var currentDbVersion = await migrationProvider.GetDbVersionSafeAsync();
            await migrationProvider.CloseConnectionAsync();
            
            Assert.Equal(new DbVersion(3,0), currentDbVersion);
        }
    }
}