namespace Mando.Api.Configurations;

public class StartupExecutionSettings
{
    public const string SectionName = "Startup";

    public bool ApplyMigrationsOnStartup { get; set; }
    public bool RunSeedOnStartup { get; set; }
}