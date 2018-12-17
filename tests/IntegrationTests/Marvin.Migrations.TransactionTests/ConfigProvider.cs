using Marvin.Migrations.PostgreSql.IntegrationTests;
using Microsoft.Extensions.Configuration;

namespace Marvin.Migrations.TransactionTests
{
    public static class ConfigProvider
    {
        public static Config GetConfig() => new ConfigurationBuilder()
            .AddYamlFile("config.yml")
            .Build()
            .Get<Config>();
    }
}