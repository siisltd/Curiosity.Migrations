namespace Curiosity.Migrations
{
    /// <summary>
    /// Options for <see cref="IDbProvider"/>
    /// </summary>
    public interface IDbProviderOptions
    {
        /// <summary>
        /// Connection string to database
        /// </summary>
        string ConnectionString { get; }

        /// <summary>
        /// Name of migration history table
        /// </summary>
        /// <remarks>
        /// If property is <see langword="null"/> <see cref="IDbProvider"/> will used default value
        /// </remarks>
        string MigrationHistoryTableName { get; }
    }
}