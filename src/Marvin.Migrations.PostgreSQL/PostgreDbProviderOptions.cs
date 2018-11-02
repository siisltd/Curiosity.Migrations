using System;

namespace Marvin.Migrations.PostgreSQL
{
    /// <summary>
    /// Options for <see cref="PostgreDbProvider"/>
    /// </summary>
    public class PostgreDbProviderOptions : IDbProviderOptions
    {
        
        public const string DefaultMigrationTableName = "MigrationHistory";
        
        public const string DefaultDatabaseEncoding = "UTF8";
        
        public const string DefaultLC_COLLATE= "Russian_Russia.1251";
        
        public const string DefaultLC_CTYPE= "Russian_Russia.1251";

        public const int DefaultConnectionLimit = -1;
        
        /// <inheritdoc />
        public string ConnectionString { get; }

        /// <inheritdoc />
        public string MigrationHistoryTableName { get; }
        
        /// <summary>
        /// Text presentation of database encoding for Postgre
        /// </summary>
        /// <remarks>
        /// Used on database creation.
        /// </remarks>
        public string DatabaseEncoding { get; }
        
        /// <summary>
        /// String sort order for Postgre
        /// </summary>
        /// <remarks>
        /// Used on database creation
        /// </remarks>
        public string LC_COLLATE { get; }
        
        /// <summary>
        /// Character classification for Postgre
        /// </summary>
        /// <remarks>
        /// Used on database creation.
        /// What is a letter? Its upper-case equivalent?)
        /// </remarks>
        public string LC_CTYPE { get; }
        
        /// <summary>
        /// Limit of connections to Postgre
        /// </summary>
        /// <remarks>
        /// Used on database creation
        /// </remarks>
        public int ConnectionLimit { get; }

        /// <inheritdoc />
        public PostgreDbProviderOptions(
            string connectionString, 
            string migrationHistoryTableName = null, 
            string databaseEncoding = null, 
            string lcCollate = DefaultLC_COLLATE, 
            string lcCtype = DefaultLC_CTYPE, 
            int? connectionLimit = DefaultConnectionLimit)
        {
            if (String.IsNullOrWhiteSpace(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            if (String.IsNullOrWhiteSpace(migrationHistoryTableName)) throw new ArgumentNullException(nameof(migrationHistoryTableName));
            if (String.IsNullOrWhiteSpace(databaseEncoding)) throw new ArgumentNullException(nameof(databaseEncoding));
            if (String.IsNullOrWhiteSpace(lcCollate)) throw new ArgumentNullException(nameof(lcCollate));
            if (String.IsNullOrWhiteSpace(lcCtype)) throw new ArgumentNullException(nameof(lcCtype));
            
            ConnectionString = connectionString;
            MigrationHistoryTableName = String.IsNullOrWhiteSpace(migrationHistoryTableName)
                ? DefaultMigrationTableName
                : migrationHistoryTableName;
            DatabaseEncoding = String.IsNullOrWhiteSpace(databaseEncoding)
                ? DefaultDatabaseEncoding
                : databaseEncoding;
            LC_COLLATE = String.IsNullOrWhiteSpace(lcCollate)
                ? DefaultLC_COLLATE
                : lcCollate;
            LC_CTYPE = String.IsNullOrWhiteSpace(lcCtype)
                ? DefaultLC_CTYPE
                : lcCtype;
            ConnectionLimit = connectionLimit ?? DefaultConnectionLimit;
        }
    }
}