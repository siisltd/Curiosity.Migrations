using System.Collections.Generic;

namespace Marvin.Migrations
{
    public interface IMigrationConfigurationProvider
    {
        ICollection<IMigrationConfiguration> GetMigrationConfigurations(string assemblyPartName);
    }
}