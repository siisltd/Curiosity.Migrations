using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Class for providing <see cref="CodeMigration"/>
    /// </summary>
    public class CodeMigrationsProvider : IMigrationsProvider
    {
        private readonly List<Assembly> _assemblies;
        private readonly Dictionary<Type, List<Assembly>> _typedAssemblies;
        private readonly IServiceCollection _services;

        public CodeMigrationsProvider(IServiceCollection services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _assemblies = new List<Assembly>();
            _typedAssemblies = new Dictionary<Type, List<Assembly>>();
        }

        /// <summary>
        /// Set up assembly for scanning migrations
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <exception cref="ArgumentNullException"></exception>
        public CodeMigrationsProvider FromAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            _assemblies.Add(assembly);

            return this;
        }

        /// <summary>
        /// Set up assembly for scanning migrations with specified type
        /// </summary>
        /// <param name="assembly">Assembly to scan</param>
        /// <exception cref="ArgumentNullException"></exception>
        public CodeMigrationsProvider FromAssembly<T>(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var migrationType = typeof(T);

            if (!_typedAssemblies.ContainsKey(migrationType))
            {
                _typedAssemblies[migrationType] = new List<Assembly>(1);
            }

            _typedAssemblies[migrationType].Add(assembly);

            return this;
        }

        /// <inheritdoc />
        public ICollection<IMigration> GetMigrations(
            IDbProvider dbProvider,
            IReadOnlyDictionary<string, string> variables,
            ILogger migrationLogger)
        {
            if (dbProvider == null) throw new ArgumentNullException(nameof(dbProvider));
            if (variables == null) throw new ArgumentNullException(nameof(variables));

            var migrations = new List<IMigration>();
            var migratorType = typeof(CodeMigration);
            var migrationsToResolve = new List<Type>();

            foreach (var assembly in _assemblies)
            {
                migrationsToResolve.AddRange(assembly
                    .GetTypes()
                    .Where(x => x.IsSubclassOf(migratorType) && !x.IsAbstract));
            }

            foreach (var keyValue in _typedAssemblies)
            {
                Func<Type, bool> selector;
                if (keyValue.Key.IsInterface)
                {
                    selector = type => type.GetInterfaces().Contains(keyValue.Key);
                }
                else
                {
                    selector = type => type.IsSubclassOf(keyValue.Key);
                }

                foreach (var assembly in keyValue.Value)
                {
                    migrationsToResolve.AddRange(assembly
                        .GetTypes()
                        .Where(x => x.IsSubclassOf(migratorType) && !x.IsAbstract)
                        .Where(selector));
                }
            }

            foreach (var migrationType in migrationsToResolve)
            {
                _services.AddTransient(migrationType);
            }

            var serviceProvider = _services.BuildServiceProvider();

            foreach (var migrationToResolve in migrationsToResolve)
            {
                if (!(serviceProvider.GetRequiredService(migrationToResolve) is CodeMigration migration))
                    throw new InvalidOperationException($"{migrationToResolve.GetType()} no created");

                migration.Init(dbProvider, variables, migrationLogger);
                migrations.Add(migration);
            }

            var migrationCheckMap = new HashSet<DbVersion>();
            foreach (var migration in migrations)
            {
                if (migrationCheckMap.Contains(migration.Version))
                    throw new InvalidOperationException(
                        $"There is more than one migration with version {migration.Version}");

                migrationCheckMap.Add(migration.Version);
            }

            return migrations.OrderBy(x => x.Version).ToList();
        }
    }
}