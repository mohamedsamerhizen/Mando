using Mando.Api.Enums;

namespace Mando.Api.Helpers;

public static class OperationsAlertIdentityHelper
{
    public static string BuildAlertKey(
        OperationsAlertCategory category,
        OperationsAlertEntityType entityType,
        Guid entityId,
        string? qualifier = null)
    {
        var key = $"{(int)category}:{(int)entityType}:{entityId:N}";
        return string.IsNullOrWhiteSpace(qualifier)
            ? key
            : $"{key}:{NormalizeQualifier(qualifier)}";
    }

    public static string BuildAlertFingerprint(string alertKey, DateTime triggeredAtUtc)
    {
        return $"{alertKey}|{triggeredAtUtc.Ticks}";
    }

    public static bool TryParseFingerprint(string alertFingerprint, out ParsedOperationsAlertFingerprint parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(alertFingerprint))
            return false;

        var separatorIndex = alertFingerprint.LastIndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == alertFingerprint.Length - 1)
            return false;

        var alertKey = alertFingerprint[..separatorIndex];
        var ticksSegment = alertFingerprint[(separatorIndex + 1)..];

        if (!long.TryParse(ticksSegment, out var ticks))
            return false;

        if (!TryParseAlertKey(alertKey, out var parsedKey))
            return false;

        parsed = new ParsedOperationsAlertFingerprint(
            alertFingerprint,
            alertKey,
            parsedKey.Category,
            parsedKey.EntityType,
            parsedKey.EntityId,
            parsedKey.Qualifier,
            new DateTime(ticks, DateTimeKind.Utc));

        return true;
    }

    public static string NormalizeQualifier(string qualifier)
    {
        return PaymentReferenceNormalizer.Normalize(qualifier)
               ?? throw new ArgumentException("Qualifier cannot be empty after normalization.", nameof(qualifier));
    }

    private static bool TryParseAlertKey(string alertKey, out ParsedOperationsAlertKey parsed)
    {
        parsed = default;

        if (string.IsNullOrWhiteSpace(alertKey))
            return false;

        var segments = alertKey.Split(':', 4, StringSplitOptions.None);
        if (segments.Length < 3)
            return false;

        if (!int.TryParse(segments[0], out var categoryValue) ||
            !Enum.IsDefined(typeof(OperationsAlertCategory), categoryValue))
        {
            return false;
        }

        if (!int.TryParse(segments[1], out var entityTypeValue) ||
            !Enum.IsDefined(typeof(OperationsAlertEntityType), entityTypeValue))
        {
            return false;
        }

        if (!Guid.TryParseExact(segments[2], "N", out var entityId))
            return false;

        var qualifier = segments.Length == 4 ? segments[3] : null;

        parsed = new ParsedOperationsAlertKey(
            (OperationsAlertCategory)categoryValue,
            (OperationsAlertEntityType)entityTypeValue,
            entityId,
            qualifier);

        return true;
    }

    public readonly record struct ParsedOperationsAlertKey(
        OperationsAlertCategory Category,
        OperationsAlertEntityType EntityType,
        Guid EntityId,
        string? Qualifier);

    public readonly record struct ParsedOperationsAlertFingerprint(
        string AlertFingerprint,
        string AlertKey,
        OperationsAlertCategory Category,
        OperationsAlertEntityType EntityType,
        Guid EntityId,
        string? Qualifier,
        DateTime TriggeredAtUtc);
}