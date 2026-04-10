using Mando.Api.DTOs.Payments;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Payments;

namespace Mando.Api.Interfaces.Payments;

public interface IPaymentWorkflowService
{
    Task<PaymentWorkflowResult> CreateAsync(CreatePaymentRequestDto request, AppUser currentUser);

    Task<PaymentWorkflowResult> ApproveAsync(
        Guid paymentId,
        ApprovePaymentRequestDto request,
        AppUser currentUser);

    Task<PaymentWorkflowResult> RejectAsync(
        Guid paymentId,
        RejectPaymentRequestDto request,
        AppUser currentUser);

    Task<PaymentWorkflowResult> ReverseApprovedAsync(
        Guid paymentId,
        ReversePaymentRequestDto request,
        AppUser currentUser);

    Task<PaymentWorkflowResult> VoidApprovedAsync(
        Guid paymentId,
        VoidApprovedPaymentRequestDto request,
        AppUser currentUser);
}