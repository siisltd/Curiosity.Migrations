using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations;

/// <summary>
/// Builder for <see cref="IMigrationEngine"/>
/// </summary>
/// <remarks>
/// Configures how instance of <see cref="IMigrationEngine"/> should work:
/// which migrations should be used, what is desired target version, what are the restrictions, etc. 
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class MigrationEngineBuilder
{
    private MigrationPolicy _upgradePolicy;
    private MigrationPolicy _downgradePolicy;

    private readonly ICollection<IMigrationsProvider> _migrationsProviders;
    private readonly ICollection<IMigrationsProvider> _preMigrationsProviders;

    private IMigrationConnectionFactory? _dbProviderFactory;
    private MigrationVersion? _targetVersion;
    private ILogger? _logger;

    /// <summary>
    /// Dictionary with variables
    /// </summary>
    /// <remarks>
    /// Key = variable name, value - variable value
    /// </remarks>
    private readonly Dictionary<string, string> _variables;

    private readonly IServiceCollection _services;

    /// <summary>
    /// Logger for sql queries
    /// </summary>
    private ILogger? _sqlLogger;

    /// <inheritdoc cref="MigrationEngineBuilder"/>
    /// <param name="services">Existed instance of <see cref="IServiceCollection"/> that should be used for internal dependency injection. If null, new empty instance will be created.</param>
    public MigrationEngineBuilder(IServiceCollection? services = null)
    {
        _services = services ?? new ServiceCollection();
        _migrationsProviders = new List<IMigrationsProvider>();
        _preMigrationsProviders = new List<IMigrationsProvider>();
        _upgradePolicy = MigrationPolicy.AllAllowed;
        _downgradePolicy = MigrationPolicy.AllForbidden;
        _targetVersion = default;
        _variables = new Dictionary<string, string>();
    }

    /// <summary>
    /// Allows to add <see cref="ScriptMigration"/> migrations from sql files
    /// </summary>
    /// <returns>Provider of <see cref="ScriptMigration"/></returns>
    public ScriptMigrationsProvider UseScriptMigrations()
    {
        var scriptMigrationProvider = new ScriptMigrationsProvider();
        _migrationsProviders.Add(scriptMigrationProvider);
        return scriptMigrationProvider;
    }

    /// <summary>
    /// Allows to add <see cref="ScriptMigration"/> migrations from sql files that will be executed before main migration
    /// </summary>
    /// <returns>Provider of <see cref="ScriptMigration"/></returns>
    public ScriptMigrationsProvider UseScriptPreMigrations()
    {
        var scriptMigrationProvider = new ScriptMigrationsProvider();
        _preMigrationsProviders.Add(scriptMigrationProvider);
        return scriptMigrationProvider;
    }

    /// <summary>
    /// Allows to add <see cref="CodeMigration"/> migrations from assembly
    /// </summary>
    /// <returns>Provider of <see cref="CodeMigration"/></returns>
    public CodeMigrationsProvider UseCodeMigrations()
    {
        var codeMigrationProvider = new CodeMigrationsProvider(_services);
        _migrationsProviders.Add(codeMigrationProvider);
        return codeMigrationProvider;
    }

    /// <summary>
    /// Allows to add <see cref="CodeMigration"/> scripts that will be executed before main migration
    /// </summary>
    /// <returns>Provider of <see cref="CodeMigration"/></returns>
    public CodeMigrationsProvider UseCodePreMigrations()
    {
        var codeMigrationProvider = new CodeMigrationsProvider(_services);
        _preMigrationsProviders.Add(codeMigrationProvider);
        return codeMigrationProvider;
    }

    /// <summary>
    /// Allows to use custom migration provider
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public MigrationEngineBuilder UseCustomMigrationsProvider(IMigrationsProvider provider)
    {
        Guard.AssertNotNull(provider, nameof(provider));

        _migrationsProviders.Add(provider);
        return this;
    }

    /// <summary>
    /// Setup upgrade migration policy
    /// </summary>
    /// <param name="policy">Policy</param>
    public MigrationEngineBuilder UseUpgradeMigrationPolicy(MigrationPolicy policy)
    {
        _upgradePolicy = policy;
        return this;
    }

    /// <summary>
    /// Setup downgrade migration policy
    /// </summary>
    /// <param name="policy">Policy</param>
    public MigrationEngineBuilder UseDowngradeMigrationPolicy(MigrationPolicy policy)
    {
        _downgradePolicy = policy;
        return this;
    }

    /// <summary>
    /// Uses specified logger for migrator. This logger will be used to logging internal logs of migrator. 
    /// </summary>
    public MigrationEngineBuilder UseLogger(ILogger logger)
    {
        Guard.AssertNotNull(logger, nameof(logger));

        _logger = logger;
        return this;
    }

    /// <summary>
    /// Use specified logger to log sql queries. All executed sql queries will be logged into specified logger.
    /// </summary>
    public MigrationEngineBuilder UseLoggerForSql(ILogger? logger)
    {
        _sqlLogger = logger;
        return this;
    }

    /// <summary>
    /// Setup factory for migration connection.
    /// </summary>
    public MigrationEngineBuilder UseMigrationConnectionFactory(IMigrationConnectionFactory migrationConnectionFactory)
    {
        Guard.AssertNotNull(migrationConnectionFactory, nameof(migrationConnectionFactory));

        _dbProviderFactory = migrationConnectionFactory;

        return this;
    }

    /// <summary>
    /// Setup target version of migration
    /// </summary>
    /// <param name="targetMigrationVersion">Target database version</param>
    /// <returns></returns>
    /// <remarks>
    /// If <paramref name="targetMigrationVersion"></paramref> is not specified, migrator will upgrade database to the most newest migration, provided by <see cref="IMigrationsProvider"/>
    /// If <paramref name="targetMigrationVersion"></paramref> is specified, migrator will upgrade or downgrade database depending on the current DB version and the specified
    /// </remarks>
    public MigrationEngineBuilder SetUpTargetVersion(MigrationVersion targetMigrationVersion)
    {
        _targetVersion = targetMigrationVersion;
        return this;
    }

    /// <summary>
    /// Adds specified variables that will be passed to code migrations and auto substitute to script migrations.
    /// </summary>
    /// <param name="name">Variable name</param>
    /// <param name="value">Variable value</param>
    /// <exception cref="ArgumentNullException">If any of arguments is <see langword="null"/> or empty</exception>
    /// <remarks>
    /// The variable will be overwritten if it was added before.
    /// </remarks>
    public MigrationEngineBuilder UseVariable(string name, string value)
    {
        Guard.AssertNotEmpty(name, nameof(name));
        Guard.AssertNotEmpty(value, nameof(value));

        _variables[name] = value;

        return this;
    }
        
    /// <summary>
    /// Builds migrator.
    /// </summary>
    /// <returns>Configured and ready new instance of <see cref="IMigrationEngine"/>.</returns>
    /// <exception cref="InvalidOperationException">Throws when <see cref="IMigrationConnection"/> or <see cref="IMigration"/> does not specified</exception>
    public IMigrationEngine Build()
    {
        if (_dbProviderFactory == null)
            throw new InvalidOperationException(
                $"{typeof(IMigrationConnection)} not set up. Use {nameof(UseMigrationConnectionFactory)}");
        if (_migrationsProviders.Count == 0)
            throw new InvalidOperationException(
                $"{typeof(IMigrationsProvider)} not set up. Use {nameof(UseScriptMigrations)} or {nameof(UseCodeMigrations)}");

        var dbProvider = _dbProviderFactory.CreateMigrationConnection();
        dbProvider.UseSqlLogger(_sqlLogger);
            
        var providerVariables = dbProvider.GetDefaultVariables();
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

        return new MigrationEngine(
            dbProvider,
            migrations,
            _upgradePolicy,
            _downgradePolicy,
            preMigrations,
            _targetVersion,
            _logger);
    }
}
