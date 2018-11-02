using System.Collections.Generic;

namespace Marvin.Migrations
{
    /// <summary>
    /// Class for providing migrations from different sources
    /// </summary>
    public interface IMigrationsProvider
    {
        /// <summary>
        /// Provide migrations
        /// </summary>
        /// <param name="dbProvider">Instance of <see cref="IDbProvider"/> to initialize migrations</param>
        /// <returns></returns>
        ICollection<IMigration> GetMigrations(IDbProvider dbProvider);
    }
}