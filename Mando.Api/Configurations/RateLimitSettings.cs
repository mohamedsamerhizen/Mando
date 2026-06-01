using System.ComponentModel.DataAnnotations;

namespace Mando.Api.Configurations;

public static class RateLimitPolicyNames
{
    public const string Login = "login";
    public const string SensitiveMutation = "sensitive-mutation";
}

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicySettings Login { get; set; } = new()
    {
        PermitLimit = 10,
        WindowSeconds = 60
    };

    public RateLimitPolicySettings SensitiveMutation { get; set; } = new()
    {
        PermitLimit = 30,
        WindowSeconds = 60
    };
}

public sealed class RateLimitPolicySettings
{
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; }

    [Range(1, 86_400)]
    public int WindowSeconds { get; set; }
}
