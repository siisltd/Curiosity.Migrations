namespace Marvin.Migrations
{
    public enum DbState
    {
        Unknown = 0,
        NotCreated = 1,
        Outdated = 2,
        Newer = 3,
        Ok = 4
    }
}