using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Marvin.Migrations.Migrations;

namespace Marvin.Migrations.MigrationProviders
{
    /// <summary>
    /// 
    /// </summary>
    public class CodeMigrationsProvider : IMigrationsProvider
    {
        private readonly List<Assembly> _assemblies;
        private readonly Dictionary<Type, List<Assembly>> _typedAssemblies;

        public CodeMigrationsProvider()
        {
            _assemblies = new List<Assembly>();
            _typedAssemblies = new Dictionary<Type, List<Assembly>>();
        }
        
        /// <summary>
        /// Set up assembly for scanning migrations
        /// </summary>
        /// <param name="assembly"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void FromAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            _assemblies.Add(assembly);
        }
        
        public void FromAssembly<T>(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            
            var migrationType = typeof(T);
            
            if (!_typedAssemblies.ContainsKey(migrationType))
            {
                _typedAssemblies[migrationType]  =new List<Assembly>(1);
            }

            _typedAssemblies[migrationType].Add(assembly);
        }
        
        public ICollection<IMigration> GetMigrations(IDbProvider dbProvider)
        {
            var migrations = new List<IMigration>();
            var migratorType = typeof(CodeMigration);
            foreach (var assembly in _assemblies)
            {
                migrations.AddRange(assembly
                    .GetTypes()
                    .Where(x => x.IsAssignableFrom(migratorType))
                    .Select(GetMigration)
                    .ToList());
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
                    selector = type => type == keyValue.Key;
                }

                foreach (var assembly in keyValue.Value)
                { 
                    migrations.AddRange(assembly
                    .GetTypes()
                    .Where(x => x.IsAssignableFrom(migratorType))
                    .Where(selector)
                    .Select(GetMigration)
                    .ToList());
                }
            }
            var  migrationCheckMap = new HashSet<DbVersion>();
            foreach (var migration in migrations)
            {
                if (migrationCheckMap.Contains(migration.Version))
                    throw new InvalidOperationException(
                        $"There is more than one migration with version {migration.Version}");

                migrationCheckMap.Add(migration.Version);
            }

            return migrations;
        }
        
        private IMigration GetMigration(Type type)
        {
            return (CodeMigration)Activator.CreateInstance(type);
        }
    }
}