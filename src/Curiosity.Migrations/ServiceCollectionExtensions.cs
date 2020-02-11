using System;
using Microsoft.Extensions.DependencyInjection;

namespace Curiosity.Migrations
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/>
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds migrations to services
        /// </summary>
        /// <param name="service">IoC</param>
        /// <param name="options">Migration options</param>
        /// <returns></returns>
        public static IServiceCollection AddMigrations(this IServiceCollection service, Action<MigratorBuilder> options)
        {
            var builder = new MigratorBuilder(service);
            options.Invoke(builder);
            var migrator = builder.Build();
            service.AddSingleton(migrator);
            return service;
        }
    }
}