using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Payments;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Payments;

namespace Mando.Api.Interfaces.Payments;

public interface IPaymentQueryService
{
    Task<PaymentQueryResult<PagedResultDto<PaymentResponseDto>>> GetAllAsync(
        GetPaymentsQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<PaymentQueryResult<PaymentResponseDto>> GetByIdAsync(
        Guid paymentId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<PaymentQueryResult<IReadOnlyList<PaymentActionHistoryResponseDto>>> GetHistoryAsync(
        Guid paymentId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<PaymentQueryResult<PaymentReviewQueueResponseDto>> GetReviewQueueAsync(
        GetPaymentReviewQueueQueryDto query);

    Task<PaymentQueryResult<PaymentOperationsReportResponseDto>> GetOperationsReportAsync(
        GetPaymentOperationsReportQueryDto query);
}