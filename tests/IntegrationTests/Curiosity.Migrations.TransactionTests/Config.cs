namespace Curiosity.Migrations.TransactionTests
{
    public class Config
    {
        /// <summary>
        /// Маска строки подключения к БД. Должно быть указано все, кроме имя БД
        /// </summary>
        public string ConnectionStringMask { get; set; }
    }
}