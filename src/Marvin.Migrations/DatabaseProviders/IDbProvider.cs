using System.Threading.Tasks;
using Marvin.Migrations.Info;

namespace Marvin.Migrations.DatabaseProviders
{
    public interface IDbProvider
    {
        string DbName { get; }

        Task<DbState> GetDbStateAsync(DbVersion desiredVersion);
        
        Task CreateDatabaseIfNotExistsAsync();

        Task CreateHistoryTableIfNotExistsAsync();

        Task<DbVersion?> GetDbVersionAsync();

        Task UpdateCurrentDbVersionAsync(DbVersion version);

        Task ExecuteScriptAsync(string script);
        
        Task<object> ExecuteScalarScriptAsync(string script);
        
        Task ExecuteScriptWithoutInitialCatalogAsync(string script);
        
        Task<object> ExecuteScalarScriptWithoutInitialCatalogAsync(string script);
    }
}