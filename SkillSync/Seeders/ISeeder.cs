namespace SkillSync.Seeders
{
    public enum SeederEnvironment
    {
        Development,
        Production,
        Both
    }

    public interface ISeeder
    {
        SeederEnvironment Environment { get; }
        Task SeedAsync(int rowsCount, SeederEnvironment currentEnvironment);
    }
}