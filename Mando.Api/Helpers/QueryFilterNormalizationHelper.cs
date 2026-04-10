using System.Text;

namespace Mando.Api.Helpers;

public static class QueryFilterNormalizationHelper
{
    public static string? NormalizeSingleLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

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

        return builder.Length == 0
            ? null
            : builder.ToString();
    }

    public static string? NormalizeDigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsDigit(character))
                builder.Append(character);
        }

        return builder.Length == 0
            ? null
            : builder.ToString();
    }

    public static string? NormalizeUpperInvariant(string? value)
    {
        var normalized = NormalizeSingleLine(value);
        return normalized?.ToUpperInvariant();
    }
}
