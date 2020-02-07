using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Builder for <see cref="IDbMigrator"/>
    /// </summary>
    public class MigratorBuilder
    {
        private MigrationPolicy _upgradePolicy;
        private MigrationPolicy _downgradePolicy;

        private readonly ICollection<IMigrationsProvider> _migrationsProviders;
        private readonly ICollection<IMigrationsProvider> _preMigrationsProviders;

        private IDbProviderFactory _dbProviderFactory;
        private DbVersion? _targetVersion;
        private ILogger _logger;

        /// <summary>
        /// Dictionary with variables
        /// </summary>
        /// <remarks>
        /// Key = variable name, value - variable value
        /// </remarks>
        private readonly Dictionary<string, string> _variables;

        private readonly IServiceCollection _services;

        public MigratorBuilder(IServiceCollection services = null)
        {
            _services = services ?? new ServiceCollection();
            _migrationsProviders = new List<IMigrationsProvider>();
            _preMigrationsProviders = new List<IMigrationsProvider>();
            _upgradePolicy = MigrationPolicy.All;
            _downgradePolicy = MigrationPolicy.All;
            _targetVersion = default;
            _variables = new Dictionary<string, string>();
        }

        /// <summary>
        /// Allow to add <see cref="ScriptMigration"/> migrations from sql files
        /// </summary>
        /// <returns>Provider of <see cref="ScriptMigration"/></returns>
        public ScriptMigrationsProvider UseScriptMigrations()
        {
            var scriptMigrationProvider = new ScriptMigrationsProvider();
            _migrationsProviders.Add(scriptMigrationProvider);
            return scriptMigrationProvider;
        }

        /// <summary>
        /// Allow to add <see cref="ScriptMigration"/> migrations from sql files that will be executed before main migration
        /// </summary>
        /// <returns>Provider of <see cref="ScriptMigration"/></returns>
        public ScriptMigrationsProvider UseScriptPreMigrations()
        {
            var scriptMigrationProvider = new ScriptMigrationsProvider();
            _preMigrationsProviders.Add(scriptMigrationProvider);
            return scriptMigrationProvider;
        }

        /// <summary>
        /// Allow to add <see cref="CodeMigration"/> migrations from assembly
        /// </summary>
        /// <returns>Provider of <see cref="CodeMigration"/></returns>
        public CodeMigrationsProvider UseCodeMigrations()
        {
            var codeMigrationProvider = new CodeMigrationsProvider(_services);
            _migrationsProviders.Add(codeMigrationProvider);
            return codeMigrationProvider;
        }

        /// <summary>
        /// Allow to add <see cref="CodeMigration"/> scripts that will be executed before main migration
        /// </summary>
        /// <returns>Provider of <see cref="CodeMigration"/></returns>
        public CodeMigrationsProvider UseCodePreMigrations()
        {
            var codeMigrationProvider = new CodeMigrationsProvider(_services);
            _preMigrationsProviders.Add(codeMigrationProvider);
            return codeMigrationProvider;
        }

        /// <summary>
        /// Allow to use custom migration provider
        /// </summary>
        /// <param name="provider"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public MigratorBuilder UseCustomMigrationsProvider(IMigrationsProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _migrationsProviders.Add(provider);
            return this;
        }

        /// <summary>
        /// Setup upgrade migration policy
        /// </summary>
        /// <param name="policy">Policy</param>
        /// <returns></returns>
        public MigratorBuilder UseUpgradeMigrationPolicy(MigrationPolicy policy)
        {
            _upgradePolicy = policy;
            return this;
        }

        /// <summary>
        /// Setup downgrade migration policy
        /// </summary>
        /// <param name="policy">Policy</param>
        /// <returns></returns>
        public MigratorBuilder UseDowngradeMigrationPolicy(MigrationPolicy policy)
        {
            _downgradePolicy = policy;
            return this;
        }

        /// <summary>
        /// Setup logger for migrator 
        /// </summary>
        /// <param name="logger"></param>
        /// <returns></returns>
        public MigratorBuilder UserLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            return this;
        }

        /// <summary>
        /// Setup factory for provider to database access
        /// </summary>
        /// <param name="dbProviderFactory"></param>
        /// <returns></returns>
        public MigratorBuilder UserDbProviderFactory(IDbProviderFactory dbProviderFactory)
        {
            _dbProviderFactory = dbProviderFactory ?? throw new ArgumentNullException(nameof(dbProviderFactory));
            return this;
        }

        /// <summary>
        /// Setup target version of migration
        /// </summary>
        /// <param name="targetDbVersion">Target database version</param>
        /// <returns></returns>
        /// <remarks>
        /// If <paramref name="targetDbVersion"></paramref> is not specified, migrator will upgrade database to the most newest migration, provided by <see cref="IMigrationsProvider"/>
        /// If <paramref name="targetDbVersion"></paramref> is specified, migrator will upgrade or downgrade database depending on the current DB version and the specified
        /// </remarks>
        public MigratorBuilder SetUpTargetVersion(DbVersion targetDbVersion)
        {
            _targetVersion = targetDbVersion;
            return this;
        }

        /// <summary>
        /// Add specified variables that will be passed to code migrations and auto substitute to script migrations
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <param name="value">Variable value</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If any of arguments is <see langword="null"/> or empty</exception>
        public MigratorBuilder UseVariable(string name, string value)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));

            _variables[name] = value ?? throw new ArgumentNullException(nameof(value));

            return this;
        }

        /// <summary>
        /// Build migrator
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Throws when <see cref="IDbProvider"/> or <see cref="IMigration"/> does not specified</exception>
        public IDbMigrator Build()
        {
            if (_dbProviderFactory == null)
                throw new InvalidOperationException(
                    $"{typeof(IDbProvider)} not set up. Use {nameof(UserDbProviderFactory)}");
            if (_migrationsProviders.Count == 0)
                throw new InvalidOperationException(
                    $"{typeof(IMigrationsProvider)} not set up. Use {nameof(UseScriptMigrations)} or {nameof(UseCodeMigrations)}");

            var dbProvider = _dbProviderFactory.CreateDbProvider();

            var providerVariables = dbProvider.GetDefaultVariables() ?? new Dictionary<string, string>();
            foreach (var kvp in providerVariables)
            {
                // we should not override the variables set manually
                if (_variables.ContainsKey(kvp.Key)) continue;

                _variables[kvp.Key] = kvp.Value;
            }

            var migrations = new List<IMigration>();
            foreach (var migrationsProvider in _migrationsProviders)
            {
                migrations.AddRange(migrationsProvider.GetMigrations(dbProvider, _variables, _logger));
            }

            var preMigrations = new List<IMigration>();
            foreach (var migrationsProvider in _preMigrationsProviders)
            {
                preMigrations.AddRange(migrationsProvider.GetMigrations(dbProvider, _variables, _logger));
            }

            return new DbMigrator(
                dbProvider,
                migrations,
                _upgradePolicy,
                _downgradePolicy,
                preMigrations,
                _targetVersion,
                _logger);
        }
    }
}