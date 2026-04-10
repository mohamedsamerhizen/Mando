using System.Text;

namespace Mando.Api.Helpers;

public static class PaymentReferenceNormalizer
{
    public static string? Normalize(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;

        var builder = new StringBuilder(reference.Length);

        foreach (var character in reference)
        {
            if (!char.IsLetterOrDigit(character))
                continue;

            builder.Append(char.ToUpperInvariant(character));
        }

        return builder.Length == 0
            ? null
            : builder.ToString();
    }

    public static bool HasValue(string? reference)
    {
        return Normalize(reference) is not null;
    }

    public static bool AreEquivalent(string? left, string? right)
    {
        var normalizedLeft = Normalize(left);
        var normalizedRight = Normalize(right);

        return normalizedLeft is not null &&
               normalizedRight is not null &&
               normalizedLeft == normalizedRight;
    }
}