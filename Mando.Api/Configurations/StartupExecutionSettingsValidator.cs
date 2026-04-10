using Microsoft.Extensions.Options;

namespace Mando.Api.Configurations;

public sealed class StartupExecutionSettingsValidator : IValidateOptions<StartupExecutionSettings>
{
    private readonly IConfiguration _configuration;

    public StartupExecutionSettingsValidator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ValidateOptionsResult Validate(string? name, StartupExecutionSettings options)
    {
        var failures = new List<string>();

        if (!options.RunSeedOnStartup)
            return ValidateOptionsResult.Success;

        ValidateSeedAccount("SeedAdmin", failures);
        ValidateSeedAccount("SeedManager", failures);

        var salesRepSections = _configuration.GetSection("SeedSalesReps").GetChildren().ToList();
        for (var index = 0; index < salesRepSections.Count; index++)
        {
            ValidateSeedAccount($"SeedSalesReps:{index}", failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private void ValidateSeedAccount(string sectionPath, List<string> failures)
    {
        var section = _configuration.GetSection(sectionPath);

        var fullName = section["FullName"]?.Trim();
        var email = section["Email"]?.Trim();
        var password = section["Password"]?.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            failures.Add($"{sectionPath}:FullName is required when Startup:RunSeedOnStartup is true.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            failures.Add($"{sectionPath}:Email is required when Startup:RunSeedOnStartup is true.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            failures.Add($"{sectionPath}:Password is required when Startup:RunSeedOnStartup is true.");
        }
    }
}
