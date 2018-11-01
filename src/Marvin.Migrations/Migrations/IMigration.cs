using System.Threading.Tasks;

namespace Marvin.Migrations.Migrations
{
    public interface IMigration
    {
        /// <summary>
        /// Migration version
        /// </summary>
        DbVersion Version { get; }
        
        string Comment { get; }

        Task UpgradeAsync();

        Task DowngradeAsync();
    }
}