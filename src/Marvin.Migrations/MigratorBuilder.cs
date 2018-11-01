using System;
using System.Collections.Generic;
using Marvin.Migrations.MigrationProviders;
using Marvin.Migrations.Migrations;
using Microsoft.Extensions.Logging;

namespace Marvin.Migrations
{
    public class MigratorBuilder
    {
        private readonly List<IMigrationsProvider> _migrationsProviders;
        private IDbProvider _dbProvider;

        private AutoMigrationPolicy _upgradePolicy;
        private AutoMigrationPolicy _downgradePolicy;
        private DbVersion? _targetVersion;

        private ILogger _logger;
        
        public MigratorBuilder()
        {
            _migrationsProviders = new List<IMigrationsProvider>();
        }
        
        
        public ScriptMigrationsProvider UseScriptMigrations()
        {
            var scriptMigrationProvider = new ScriptMigrationsProvider();
            _migrationsProviders.Add(scriptMigrationProvider);
            return scriptMigrationProvider;
        }

        public CodeMigrationsProvider UseCodeMigrations()
        {
            return null;
        }


        public MigratorBuilder UseUpgradeAutoMigrationPolicy(AutoMigrationPolicy policy)
        {
            _upgradePolicy = policy;
            return this;
        } 
        public MigratorBuilder UseDowngradeAutoMigrationPolicy(AutoMigrationPolicy policy)
        {
            _downgradePolicy = policy;
            return this;
        }

        public MigratorBuilder UserLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }

        public MigratorBuilder UserDbProvider(IDbProvider dbProvider)
        {
            _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            return this;
        }

        public MigratorBuilder SetUpTargetVersion(DbVersion targetDbVersion)
        {
            _targetVersion = targetDbVersion;
            return this;
        }

        public IDbMigrator Build()
        {
            if (_dbProvider == null) throw new InvalidOperationException($"{typeof(IDbProvider)} not set up. Use {nameof(UserDbProvider)}");
            if (_migrationsProviders.Count == 0) throw new InvalidOperationException($"{typeof(IMigrationsProvider)} not set up. Use {nameof(UseScriptMigrations)} or {nameof(UseCodeMigrations)}");
            
            var migrations = new List<IMigration>();
            foreach (var migrationsProvider in _migrationsProviders)
            {
                migrations.AddRange(migrationsProvider.GetMigrations(_dbProvider));
            }
            
            return new DbMigrator(
                _dbProvider, 
                migrations, 
                _upgradePolicy, 
                _downgradePolicy, 
                _targetVersion, 
                _logger);
        }

    }
}