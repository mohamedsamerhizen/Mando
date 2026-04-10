using System.Net;
using Microsoft.Extensions.Options;

namespace Mando.Api.Configurations;

public sealed class ForwardedHeadersSettingsValidator : IValidateOptions<ForwardedHeadersSettings>
{
    public ValidateOptionsResult Validate(string? name, ForwardedHeadersSettings options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        if (options.KnownProxies is null || options.KnownProxies.Count == 0)
        {
            return ValidateOptionsResult.Fail(
                "ForwardedHeaders:KnownProxies must contain at least one trusted proxy when forwarded headers are enabled.");
        }

        var failures = new List<string>();

        for (var index = 0; index < options.KnownProxies.Count; index++)
        {
            var value = options.KnownProxies[index]?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                failures.Add($"ForwardedHeaders:KnownProxies:{index} is required.");
                continue;
            }

            if (!IPAddress.TryParse(value, out _))
            {
                failures.Add($"ForwardedHeaders:KnownProxies:{index} must be a valid IP address.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
