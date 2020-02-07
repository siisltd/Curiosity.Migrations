using System;

// ReSharper disable InconsistentNaming

namespace Curiosity.Migrations.PostgreSQL
{
    /// <summary>
    /// Options for <see cref="PostgreDbProvider"/>
    /// </summary>
    public class PostgreDbProviderOptions : IDbProviderOptions
    {
        /// <summary>
        /// Default value for <see cref="MigrationHistoryTableName"/>
        /// </summary>
        public const string DefaultMigrationTableName = "MigrationHistory";

        /// <inheritdoc />
        public string ConnectionString { get; }

        /// <inheritdoc />
        public string MigrationHistoryTableName { get; }

        /// <summary>
        /// Character set encoding to use in the new database. Specify a string constant (e.g., 'SQL_ASCII'), or an integer encoding number, or DEFAULT to use the default encoding (namely, the encoding of the template database).
        /// If <see langword="null"/>, Migrator will use default value (encoding of the template database).
        /// </summary>
        /// <remarks>
        /// Used on database creation.
        /// </remarks>
        public string DatabaseEncoding { get; }

        /// <summary>
        /// Collation order (LC_COLLATE) to use in the new database. This affects the sort order applied to strings, e.g. in queries with ORDER BY, as well as the order used in indexes on text columns.
        /// If <see langword="null"/>, Migrator will use default value from template database
        /// </summary>
        /// <remarks>
        /// Used on database creation
        /// </remarks>
        public string LC_COLLATE { get; }

        /// <summary>
        /// Character classification (LC_CTYPE) to use in the new database. This affects the categorization of characters, e.g. lower, upper and digit. The default is to use the character classification of the template database.
        /// If <see langword="null"/>, Migrator will use default value from template database
        /// </summary>
        /// <remarks>
        /// Used on database creation.
        /// </remarks>
        public string LC_CTYPE { get; }

        /// <summary>
        /// How many concurrent connections can be made to this database.
        /// If <see langword="null"/>, Migrator will use default value from DB (-1, means no limit)
        /// </summary>
        /// <remarks>
        /// Used on database creation
        /// </remarks>
        public int? ConnectionLimit { get; }

        /// <summary>
        /// The name of the template from which to create the new database.
        /// If <see langword="null"/>, Migrator will use the default template from DB (template1)
        /// </summary>
        public string Template { get; }

        /// <summary>
        /// The name of the tablespace that will be associated with the new database. This tablespace will be the default tablespace used for objects created in this database.
        /// If <see langword="null"/>, Migrator will use default value from db
        /// </summary>
        public string TableSpace { get; }

        public PostgreDbProviderOptions(
            string connectionString,
            string migrationHistoryTableName = null,
            string databaseEncoding = null,
            string lcCollate = null,
            string lcCtype = null,
            int? connectionLimit = null,
            string template = null,
            string tableSpace = null)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));

            ConnectionString = connectionString;
            MigrationHistoryTableName = String.IsNullOrWhiteSpace(migrationHistoryTableName)
                ? DefaultMigrationTableName
                : migrationHistoryTableName;
            DatabaseEncoding = databaseEncoding;
            LC_COLLATE = lcCollate;
            LC_CTYPE = lcCtype;
            ConnectionLimit = connectionLimit;
            Template = template;
            TableSpace = tableSpace;
        }
    }
}