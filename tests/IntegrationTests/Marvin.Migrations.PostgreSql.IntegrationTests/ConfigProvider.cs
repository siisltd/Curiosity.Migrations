using Microsoft.Extensions.Configuration;

namespace Marvin.Migrations.PostgreSql.IntegrationTests
{
    public static class ConfigProvider
    {
        public static Config GetConfig() => new ConfigurationBuilder()
            .AddYamlFile("config.yml")
            .Build()
            .Get<Config>();
    }
}