using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Marvin.Migrations
{
    public class AssemblyMigrationConfigurationProvider : IMigrationConfigurationProvider
    {
        public ICollection<IMigrationConfiguration> GetMigrationConfigurations(string assemblyPartName)
        {
            return GetAssemblies()
                .Where(x => x.FullName.Contains(assemblyPartName))
                .SelectMany(s => s.GetTypes()).Where(x => x.GetInterfaces().Contains(typeof(IMigrationConfiguration)))
                .Distinct(new TypeEqualityComparer()).Select(GetConfiguration).ToList();
        }

        private ICollection<Assembly> GetAssemblies()
        {
            var dict = new Dictionary<string, Assembly>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null) continue;
                dict[assembly.FullName] = assembly;
            }

            return dict.Values;

        }

        private IMigrationConfiguration GetConfiguration(Type type)
        {
            return (IMigrationConfiguration)Activator.CreateInstance(type);
        }

        private class TypeEqualityComparer : IEqualityComparer<Type>
        {
            public bool Equals(Type x, Type y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;

                return x.FullName == y.FullName;
            }

            public int GetHashCode(Type obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}