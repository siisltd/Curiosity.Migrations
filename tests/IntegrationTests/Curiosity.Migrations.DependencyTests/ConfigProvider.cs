using Microsoft.Extensions.Configuration;

namespace Curiosity.Migrations.DependencyTests;

public static class ConfigProvider
{
    public static Config GetConfig() => new ConfigurationBuilder()
        .AddYamlFile("config.yml")
        .Build()
        .Get<Config>()!;
}
