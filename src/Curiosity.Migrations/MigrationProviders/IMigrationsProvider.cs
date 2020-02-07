using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
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
        /// <param name="variables">Dictionary with variables. Key - variable name, value - variable value</param>
        /// <param name="migrationLogger">Logger for migration</param>
        /// <returns></returns>
        ICollection<IMigration> GetMigrations(
            IDbProvider dbProvider,
            IReadOnlyDictionary<string, string> variables,
            ILogger migrationLogger);
    }
}