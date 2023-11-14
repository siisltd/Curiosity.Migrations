namespace Curiosity.Migrations.IntegrationTests;

public class Config
{
    /// <summary>
    /// Database connection string mask. Everything except the database name must be specified.
    /// </summary>
    public string ConnectionStringMask { get; set; } = null!;
}
