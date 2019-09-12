using System;
using System.Data.Common;
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
        protected IDbProvider DbProvider;
        
        /// <summary>
        /// Sets up <see cref="IDbProvider"/> for code migration
        /// </summary>
        /// <param name="dbProvider"></param>
        internal void Init(
            IDbProvider dbProvider)
        {
            DbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
        }

        /// <inheritdoc />
        public abstract Task UpgradeAsync(DbTransaction transaction);

        /// <inheritdoc />
        public abstract Task DowngradeAsync(DbTransaction transaction);
    }
}