using Microsoft.Extensions.Configuration;

namespace SIISLtd.Migrations.PostgreSql.IntegrationTests
{
    public static class ConfigProvider
    {
        public static Config GetConfig() => new ConfigurationBuilder()
            .AddYamlFile("config.yml")
            .Build()
            .Get<Config>();
    }
}