using System;
using System.Threading.Tasks;
using System.Transactions;

namespace Marvin.Migrations
{
    /// <summary>
    /// Abstract class for migration with custom logic written in C#
    /// </summary>
    public abstract class CodeMigration : IMigration
    {
        /// <inheritdoc />
        public abstract DbVersion Version { get; }

        /// <inheritdoc />
        public abstract string Comment { get; }
        
        /// <summary>
        /// Provide access to underlying database
        /// </summary>
        protected readonly IDbProvider DbProvider;
        
        /// <inheritdoc />
        protected CodeMigration(
            IDbProvider dbProvider)
        {
            DbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        /// <inheritdoc />
        public abstract Task UpgradeAsync(CommittableTransaction transaction);

        /// <inheritdoc />
        public abstract Task DowngradeAsync(CommittableTransaction transaction);
    }
}