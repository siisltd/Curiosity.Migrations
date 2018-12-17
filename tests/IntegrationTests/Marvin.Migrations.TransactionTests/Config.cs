namespace Marvin.Migrations.PostgreSql.IntegrationTests
{
    public class Config
    {
        /// <summary>
        /// Маска строки подключения к БД. Должно быть указано все, кроме имя БД
        /// </summary>
        public string ConnectionStringMask { get; set; }
    }
}