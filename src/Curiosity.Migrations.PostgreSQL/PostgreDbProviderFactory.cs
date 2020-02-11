using System;

namespace Curiosity.Migrations.PostgreSQL
{
    /// <summary>
    /// Factory for creation <see cref="PostgreDbProvider"/>
    /// </summary>
    public class PostgreDbProviderFactory : IDbProviderFactory
    {
        private readonly PostgreDbProviderOptions _options;

        public PostgreDbProviderFactory(PostgreDbProviderOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        public IDbProvider CreateDbProvider()
        {
            return new PostgreDbProvider(_options);
        }
    }
}