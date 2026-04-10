using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.Interfaces.Common;

namespace Mando.Api.Services.Common;

public class DocumentNumberGenerator : IDocumentNumberGenerator
{
    private const int MaxRetries = 10;

    private readonly AppDbContext _context;

    public DocumentNumberGenerator(AppDbContext context)
    {
        _context = context;
    }

    public Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        return GenerateUniqueAsync(
            prefix: "ORD",
            existsAsync: candidate => _context.Orders.AnyAsync(x => x.OrderNumber == candidate, cancellationToken),
            cancellationToken);
    }

    public Task<string> GeneratePaymentNumberAsync(CancellationToken cancellationToken = default)
    {
        return GenerateUniqueAsync(
            prefix: "PAY",
            existsAsync: candidate => _context.Payments.AnyAsync(x => x.PaymentNumber == candidate, cancellationToken),
            cancellationToken);
    }

    private static string BuildCandidate(string prefix)
    {
        var now = DateTime.UtcNow;
        Span<byte> randomBytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(randomBytes);
        var suffix = Convert.ToHexString(randomBytes);

        return $"{prefix}-{now:yyyyMMddHHmmssfff}-{suffix}";
    }

    private static async Task<string> GenerateUniqueAsync(
        string prefix,
        Func<string, Task<bool>> existsAsync,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var candidate = BuildCandidate(prefix);
            var exists = await existsAsync(candidate);

            if (!exists)
                return candidate;
        }

        throw new InvalidOperationException(
            $"Failed to generate a unique {prefix} document number after multiple attempts.");
    }
}
