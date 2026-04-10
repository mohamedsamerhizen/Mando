namespace Mando.Api.Interfaces.Common;

public interface IDocumentNumberGenerator
{
    Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default);
    Task<string> GeneratePaymentNumberAsync(CancellationToken cancellationToken = default);
}