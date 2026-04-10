namespace Mando.Api.Helpers;

public static class RowVersionTokenHelper
{
    public static string Encode(byte[]? rowVersion)
    {
        return rowVersion is { Length: > 0 }
            ? Convert.ToBase64String(rowVersion)
            : string.Empty;
    }

    public static bool TryDecode(string? token, out byte[] rowVersion)
    {
        rowVersion = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            rowVersion = Convert.FromBase64String(token.Trim());
            return rowVersion.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}