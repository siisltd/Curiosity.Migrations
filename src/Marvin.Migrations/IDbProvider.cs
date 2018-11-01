using System.Threading.Tasks;
using Marvin.Migrations.Exceptions;
using Marvin.Migrations.Info;

namespace Marvin.Migrations
{
    public interface IDbProvider
    {
        string DbName { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="desiredVersion"></param>
        /// <returns></returns>
        Task<DbState> GetDbStateSafeAsync(DbVersion desiredVersion);
        
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="CreatingDBException"></exception>
        Task CreateDatabaseIfNotExistsAsync();

        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="CreatingHistoryTableException"></exception>
        Task CreateHistoryTableIfNotExistsAsync();

        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="MigrationException"></exception>
        Task<DbVersion?> GetDbVersionSafeAsync();

        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="MigrationException"></exception>
        Task UpdateCurrentDbVersionAsync(DbVersion version);

        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="MigrationException"></exception>
        Task ExecuteScriptAsync(string script);
        
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="MigrationException"></exception>
        Task<object> ExecuteScalarScriptAsync(string script);
        
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="MigrationException"></exception>
        Task ExecuteScriptWithoutInitialCatalogAsync(string script);
        
        /// <exception cref="ConnectionException"></exception>
        /// <exception cref="AuthorizationException"></exception>
        /// <exception cref="MigrationException"></exception>
        Task<object> ExecuteScalarScriptWithoutInitialCatalogAsync(string script);
    }
}