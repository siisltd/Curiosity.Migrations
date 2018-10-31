using System;
using System.Threading.Tasks;
using Marvin.Migrations.Migrators;

namespace Marvin.Migrations
{
    public class MigrationManager
    {
        private readonly IDbMigrator _migrator;

        private IMigrationConfigurationProvider _configurationProvider;

        public IMigrationConfigurationProvider ConfigurationProvider
        {
            get => _configurationProvider ?? new AssemblyMigrationConfigurationProvider();
            set => _configurationProvider = value;
        }

        public MigrationManager(IDbMigrator migrator)
        {
            _migrator = migrator ?? throw new ArgumentNullException(nameof(migrator));
            _configurationProvider = new AssemblyMigrationConfigurationProvider();
        }

        public async Task<bool> TryMigrateAllAsync(string assemblyPartName)
        {
            if (String.IsNullOrEmpty(assemblyPartName)) throw new ArgumentNullException(nameof(assemblyPartName));
            try
            {
                await MigrateAllAsync(assemblyPartName);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task MigrateAllAsync(string assemblyPartName)
        {
            if (String.IsNullOrEmpty(assemblyPartName)) throw new ArgumentNullException(nameof(assemblyPartName));
            if (_configurationProvider == null) 
                throw new InvalidOperationException($"{nameof(ConfigurationProvider)} not initialized. " +
                                                    $"Setup desired {typeof(IMigrationConfigurationProvider)} before migration");
            
            foreach (var migrationConfig in _configurationProvider.GetMigrationConfigurations(assemblyPartName))
            {
                await _migrator.MigrateAsync(migrationConfig.GetProvider(), migrationConfig.GetInfo());
            }
        }
    }
}