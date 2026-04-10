using System.Text;

namespace Mando.Api.Helpers;

public static class InputNormalizationHelper
{
    public static string NormalizeRequiredSingleLine(string? value)
    {
        return NormalizeOptionalSingleLine(value) ?? string.Empty;
    }

    public static string? NormalizeOptionalSingleLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return CollapseWhitespace(value);
    }

    public static string NormalizeCode(string? value)
    {
        return NormalizeRequiredSingleLine(value).ToUpperInvariant();
    }

    public static string? NormalizeOptionalMultiline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }

    private static string CollapseWhitespace(string value)
    {
        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);
        var pendingSpace = false;

        foreach (var character in trimmed)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}