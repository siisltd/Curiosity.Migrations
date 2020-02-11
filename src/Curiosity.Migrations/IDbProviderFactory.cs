namespace Curiosity.Migrations
{
    /// <summary>
    /// Factory for creation <see cref="IDbProvider"/>
    /// </summary>
    public interface IDbProviderFactory
    {
        /// <summary>
        /// Create new instance of db provider that implements <see cref="IDbProvider"/>
        /// </summary>
        /// <returns></returns>
        IDbProvider CreateDbProvider();
    }
}