using System;
using Microsoft.Extensions.DependencyInjection;

namespace Curiosity.Migrations;

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
    /// <param name="usedExistedServiceCollection">Should specified <paramref name="service"/> used as dependency injection container for migrator.</param>
    public static IServiceCollection AddMigrations(
        this IServiceCollection service,
        Action<MigratorBuilder> options,
        bool usedExistedServiceCollection = true)
    {
        if (service == null) throw new ArgumentNullException(nameof(service));
        if (options == null) throw new ArgumentNullException(nameof(options));

        var builder = usedExistedServiceCollection
            ? new MigratorBuilder(service)
            : new MigratorBuilder();
        options.Invoke(builder);
        var migrator = builder.Build();
        service.AddSingleton(migrator);

        return service;
    }
}