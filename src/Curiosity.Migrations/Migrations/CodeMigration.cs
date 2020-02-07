using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Curiosity.Migrations
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
        protected IDbProvider DbProvider { get; private set; }

        /// <summary>
        /// User defined variables
        /// </summary>
        protected IReadOnlyDictionary<string, string> Variables { get; private set; }

        /// <summary>
        /// Logger for migration
        /// </summary>
        /// <remarks>
        /// Can be <see langword="null"/> if logger wasn't set up
        /// </remarks>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Initializes migration 
        /// </summary>
        /// <param name="dbProvider">Provider for DB access</param>
        /// <param name="variables">Variables for migrations</param>
        /// <param name="migrationLogger">Logger for migration</param>
        internal void Init(
            IDbProvider dbProvider,
            IReadOnlyDictionary<string, string> variables,
            ILogger migrationLogger)
        {
            DbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
            Variables = variables ?? throw new ArgumentNullException(nameof(variables));
            Logger = migrationLogger;
        }

        /// <inheritdoc />
        public abstract Task UpgradeAsync(DbTransaction transaction);

        /// <inheritdoc />
        public abstract Task DowngradeAsync(DbTransaction transaction);
    }
}