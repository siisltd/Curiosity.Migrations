using System;
using System.Collections.Generic;
using System.Reflection;

namespace Marvin.Migrations
{
    public class ScriptMigrationsFromResourcesProvider : IMigrationsProvider
    {
        private readonly List<Assembly> _assemblies;

        public ScriptMigrationsFromResourcesProvider()
        {
            // usually only one item will be added
            _assemblies = new List<Assembly>(1);
        }
        
        public void FromAssembly(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            _assemblies.Add(assembly);
        }
        
        public ICollection<IMigration> GetMigrations(IDbProvider dbProvider)
        {
            var migrations = new List<IMigration>();
            if (_assemblies.Count == 0) throw new InvalidOperationException($"No assemblies specified. First use method {nameof(FromAssembly)}");
            
            
            foreach (var assembly in _assemblies)
            {
                var resourceFileNames = assembly.GetManifestResourceNames();
                
            }

            return null;
        }
    }
}