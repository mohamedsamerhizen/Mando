using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mando.Api.Common;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Payments;
using Mando.Api.Entities;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Payments;
using Mando.Api.Models.Payments;

namespace Mando.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : CurrentUserAwareControllerBase
{
    private readonly IPaymentWorkflowService _paymentWorkflowService;
    private readonly IPaymentQueryService _paymentQueryService;

    public PaymentsController(
        ICurrentUserContext currentUserContext,
        IPaymentWorkflowService paymentWorkflowService,
        IPaymentQueryService paymentQueryService)
        : base(currentUserContext)
    {
        _paymentWorkflowService = paymentWorkflowService;
        _paymentQueryService = paymentQueryService;
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.SalesRep)]
    public async Task<ActionResult<PaymentResponseDto>> Create(CreatePaymentRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _paymentWorkflowService.CreateAsync(request, currentUser);

        return result.Status switch
        {
            PaymentWorkflowStatus.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Payment!.Id },
                MapPayment(result.Payment!)),

            PaymentWorkflowStatus.InvalidAmount => ApiResponseFactory.BadRequest(
                this,
                "invalid_payment_amount",
                "Amount must be greater than zero."),

            PaymentWorkflowStatus.InvalidPaymentMethod => ApiResponseFactory.BadRequest(
                this,
                "invalid_payment_method",
                "PaymentMethod is invalid."),

            PaymentWorkflowStatus.NonCashReferenceRequired => ApiResponseFactory.BadRequest(
                this,
                "non_cash_reference_required",
                "Non-cash payments must include a reference before submission."),

            PaymentWorkflowStatus.DuplicatePendingReference => ApiResponseFactory.Conflict(
                this,
                "duplicate_pending_reference",
                "A pending payment with the same normalized reference already exists for this customer."),

            PaymentWorkflowStatus.VisitNotFound => ApiResponseFactory.BadRequest(
                this,
                "visit_not_found",
                "Visit was not found."),

            PaymentWorkflowStatus.Forbidden => Forbid(),

            PaymentWorkflowStatus.VisitNotInProgress => ApiResponseFactory.BadRequest(
                this,
                "visit_not_in_progress",
                "Payment can only be created during an active visit."),

            PaymentWorkflowStatus.CustomerInactive => ApiResponseFactory.BadRequest(
                this,
                "customer_inactive",
                "Payment cannot be created for a non-active customer."),

            PaymentWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            PaymentWorkflowStatus.NoOutstandingBalance => ApiResponseFactory.BadRequest(
                this,
                "no_outstanding_balance",
                "Customer has no outstanding balance."),

            PaymentWorkflowStatus.PaymentAmountExceedsBalance => ApiResponseFactory.BadRequest(
                this,
                "payment_amount_exceeds_balance",
                $"Payment amount cannot exceed current balance ({result.CurrentBalance:0.00})."),

            PaymentWorkflowStatus.PendingPaymentsWouldExceedBalance => ApiResponseFactory.Conflict(
                this,
                "pending_payments_would_exceed_balance",
                $"Submitting this payment would push the customer's pending submitted payments above the current outstanding balance ({result.CurrentBalance:0.00})."),

            PaymentWorkflowStatus.DuplicateSubmission => ApiResponseFactory.Conflict(
                this,
                "duplicate_submission",
                "A very similar pending payment was submitted recently for the same visit. Refresh before submitting again."),

            _ => Problem("Unexpected payment workflow result.")
        };
    }

    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<PaymentResponseDto>> Approve(Guid id, ApprovePaymentRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _paymentWorkflowService.ApproveAsync(id, request, currentUser);

        return result.Status switch
        {
            PaymentWorkflowStatus.Success => Ok(MapPayment(result.Payment!)),

            PaymentWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is required and must be a valid Base64 value."),

            PaymentWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "payment_concurrency_conflict",
                "Payment was changed by another user. Refresh and retry."),

            PaymentWorkflowStatus.ApprovalReviewCommentRequired => ApiResponseFactory.BadRequest(
                this,
                "approval_review_comment_required",
                "Approval requires a non-empty review comment."),

            PaymentWorkflowStatus.ApprovalStaleAcknowledgementRequired => ApiResponseFactory.BadRequest(
                this,
                "approval_stale_acknowledgement_required",
                "This payment is stale and requires explicit acknowledgement before approval."),

            PaymentWorkflowStatus.ApprovalHighBalanceImpactAcknowledgementRequired => ApiResponseFactory.BadRequest(
                this,
                "approval_high_balance_impact_acknowledgement_required",
                "This payment has high balance impact and requires explicit acknowledgement before approval."),

            PaymentWorkflowStatus.ApprovalMultiplePendingAcknowledgementRequired => ApiResponseFactory.BadRequest(
                this,
                "approval_multiple_pending_acknowledgement_required",
                "This customer has multiple pending payments and approval requires explicit acknowledgement."),

            PaymentWorkflowStatus.ApprovalDuplicateReferenceAcknowledgementRequired => ApiResponseFactory.BadRequest(
                this,
                "approval_duplicate_reference_acknowledgement_required",
                "A duplicate pending reference was detected and approval requires explicit acknowledgement."),

            PaymentWorkflowStatus.NonCashReferenceRequiredForApproval => ApiResponseFactory.BadRequest(
                this,
                "non_cash_reference_required_for_approval",
                "Non-cash payments must have a reference before they can be approved."),

            PaymentWorkflowStatus.PaymentNotFound => ApiResponseFactory.NotFound(
                this,
                "payment_not_found",
                "Payment was not found."),

            PaymentWorkflowStatus.PaymentAlreadyApproved => ApiResponseFactory.BadRequest(
                this,
                "payment_already_approved",
                "Payment is already approved."),

            PaymentWorkflowStatus.PaymentAlreadyRejected => ApiResponseFactory.BadRequest(
                this,
                "payment_already_rejected",
                "Rejected payments cannot be approved."),

            PaymentWorkflowStatus.PaymentNotPending => ApiResponseFactory.BadRequest(
                this,
                "payment_not_pending",
                "Only pending payments can be approved."),

            PaymentWorkflowStatus.PrivilegedSelfReviewForbidden => ApiResponseFactory.Conflict(
                this,
                "privileged_self_review_forbidden",
                "The same user who submitted this payment cannot approve it. A different privileged reviewer is required."),

            PaymentWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            PaymentWorkflowStatus.NoOutstandingBalance => ApiResponseFactory.BadRequest(
                this,
                "no_outstanding_balance",
                "Customer has no outstanding balance to approve this payment against."),

            PaymentWorkflowStatus.PaymentAmountExceedsBalance => ApiResponseFactory.BadRequest(
                this,
                "payment_amount_exceeds_balance",
                $"Payment cannot be approved because its amount ({result.Payment!.Amount:0.00}) exceeds the current outstanding balance ({result.CurrentBalance:0.00})."),

            _ => Problem("Unexpected payment approval result.")
        };
    }

    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<PaymentResponseDto>> Reject(Guid id, RejectPaymentRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _paymentWorkflowService.RejectAsync(id, request, currentUser);

        return result.Status switch
        {
            PaymentWorkflowStatus.Success => Ok(MapPayment(result.Payment!)),

            PaymentWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is required and must be a valid Base64 value."),

            PaymentWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "payment_concurrency_conflict",
                "Payment was changed by another user. Refresh and retry."),

            PaymentWorkflowStatus.RejectionReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "rejection_reason_required",
                "Rejection reason is required."),

            PaymentWorkflowStatus.RejectionCategoryRequired => ApiResponseFactory.BadRequest(
                this,
                "rejection_category_required",
                "Rejection category is required."),

            PaymentWorkflowStatus.PaymentNotFound => ApiResponseFactory.NotFound(
                this,
                "payment_not_found",
                "Payment was not found."),

            PaymentWorkflowStatus.PaymentAlreadyRejected => ApiResponseFactory.BadRequest(
                this,
                "payment_already_rejected",
                "Payment is already rejected."),

            PaymentWorkflowStatus.PaymentAlreadyApproved => ApiResponseFactory.BadRequest(
                this,
                "payment_already_approved",
                "Approved payments cannot be rejected."),

            PaymentWorkflowStatus.PaymentNotPending => ApiResponseFactory.BadRequest(
                this,
                "payment_not_pending",
                "Only pending payments can be rejected."),

            PaymentWorkflowStatus.PrivilegedSelfReviewForbidden => ApiResponseFactory.Conflict(
                this,
                "privileged_self_review_forbidden",
                "The same user who submitted this payment cannot reject it. A different privileged reviewer is required."),

            _ => Problem("Unexpected payment rejection result.")
        };
    }

    [HttpPatch("{id:guid}/reverse")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<PaymentResponseDto>> ReverseApproved(Guid id, ReversePaymentRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _paymentWorkflowService.ReverseApprovedAsync(id, request, currentUser);

        return result.Status switch
        {
            PaymentWorkflowStatus.Success => Ok(MapPayment(result.Payment!)),

            PaymentWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is required and must be a valid Base64 value."),

            PaymentWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "payment_concurrency_conflict",
                "Payment was changed by another user. Refresh and retry."),

            PaymentWorkflowStatus.ReverseReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "reverse_reason_required",
                "A non-empty reverse reason is required."),

            PaymentWorkflowStatus.PaymentNotFound => ApiResponseFactory.NotFound(
                this,
                "payment_not_found",
                "Payment was not found."),

            PaymentWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            PaymentWorkflowStatus.PaymentAlreadyReversed => ApiResponseFactory.BadRequest(
                this,
                "payment_already_reversed",
                "Payment is already reversed."),

            PaymentWorkflowStatus.PaymentAlreadyRejected => ApiResponseFactory.BadRequest(
                this,
                "payment_already_rejected",
                "Rejected payments cannot be reversed."),

            PaymentWorkflowStatus.PaymentNotApproved => ApiResponseFactory.BadRequest(
                this,
                "payment_not_approved",
                "Only approved payments can be reversed."),

            _ => Problem("Unexpected payment reverse result.")
        };
    }

    [HttpPatch("{id:guid}/void")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<PaymentResponseDto>> VoidApproved(Guid id, VoidApprovedPaymentRequestDto request)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var result = await _paymentWorkflowService.VoidApprovedAsync(id, request, currentUser);

        return result.Status switch
        {
            PaymentWorkflowStatus.Success => Ok(MapPayment(result.Payment!)),

            PaymentWorkflowStatus.InvalidConcurrencyToken => ApiResponseFactory.BadRequest(
                this,
                "invalid_row_version",
                "RowVersion is required and must be a valid Base64 value."),

            PaymentWorkflowStatus.ConcurrencyConflict => ApiResponseFactory.Conflict(
                this,
                "payment_concurrency_conflict",
                "Payment was changed by another user. Refresh and retry."),

            PaymentWorkflowStatus.VoidReasonRequired => ApiResponseFactory.BadRequest(
                this,
                "void_reason_required",
                "A non-empty void reason is required."),

            PaymentWorkflowStatus.PaymentNotFound => ApiResponseFactory.NotFound(
                this,
                "payment_not_found",
                "Payment was not found."),

            PaymentWorkflowStatus.CustomerNotFound => ApiResponseFactory.NotFound(
                this,
                "customer_not_found",
                "Customer was not found."),

            PaymentWorkflowStatus.PaymentAlreadyVoided => ApiResponseFactory.BadRequest(
                this,
                "payment_already_voided",
                "Payment is already voided."),

            PaymentWorkflowStatus.PaymentAlreadyRejected => ApiResponseFactory.BadRequest(
                this,
                "payment_already_rejected",
                "Rejected payments cannot be voided."),

            PaymentWorkflowStatus.PaymentNotApproved => ApiResponseFactory.BadRequest(
                this,
                "payment_not_approved",
                "Only approved payments can be voided."),

            _ => Problem("Unexpected payment void result.")
        };
    }

    [HttpGet("review-queue")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<PaymentReviewQueueResponseDto>> GetReviewQueue(
        [FromQuery] GetPaymentReviewQueueQueryDto query)
    {
        if (query.MaxAmount.HasValue && query.MinAmount.HasValue && query.MaxAmount.Value < query.MinAmount.Value)
        {
            return ApiResponseFactory.BadRequest(
                this,
                "invalid_amount_range",
                "MaxAmount must be greater than or equal to MinAmount.");
        }

        if (query.SubmittedToUtc.HasValue && query.SubmittedFromUtc.HasValue && query.SubmittedToUtc.Value < query.SubmittedFromUtc.Value)
        {
            return ApiResponseFactory.BadRequest(
                this,
                "invalid_submitted_range",
                "SubmittedToUtc must be greater than or equal to SubmittedFromUtc.");
        }

        var result = await _paymentQueryService.GetReviewQueueAsync(query);
        return MapQueryResult(result);
    }

    [HttpGet("operations-report")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<ActionResult<PaymentOperationsReportResponseDto>> GetOperationsReport(
        [FromQuery] GetPaymentOperationsReportQueryDto query)
    {
        if (query.DateToUtc.HasValue && query.DateFromUtc.HasValue && query.DateToUtc.Value < query.DateFromUtc.Value)
        {
            return ApiResponseFactory.BadRequest(
                this,
                "invalid_report_range",
                "DateToUtc must be greater than or equal to DateFromUtc.");
        }

        var result = await _paymentQueryService.GetOperationsReportAsync(query);
        return MapQueryResult(result);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<PaymentResponseDto>>> GetAll([FromQuery] GetPaymentsQueryDto query)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _paymentQueryService.GetAllAsync(query, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PaymentResponseDto>> GetById(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _paymentQueryService.GetByIdAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<PaymentActionHistoryResponseDto>>> GetHistory(Guid id)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser is null)
            return Unauthorized();

        var currentUserRoles = await GetCurrentUserRolesAsync(currentUser);
        var result = await _paymentQueryService.GetHistoryAsync(id, currentUser, currentUserRoles);

        return MapQueryResult(result);
    }

    private ActionResult<T> MapQueryResult<T>(PaymentQueryResult<T> result)
    {
        switch (result.Status)
        {
            case PaymentQueryStatus.Success:
                return Ok(result.Data);

            case PaymentQueryStatus.PaymentNotFound:
                return new ActionResult<T>(ApiResponseFactory.NotFound(
                    this,
                    "payment_not_found",
                    "Payment was not found."));

            case PaymentQueryStatus.Forbidden:
                return new ActionResult<T>(Forbid());

            default:
                return new ActionResult<T>(Problem("Unexpected payment query result."));
        }
    }

    private static PaymentResponseDto MapPayment(Payment payment)
    {
        return new PaymentResponseDto
        {
            Id = payment.Id,
            PaymentNumber = payment.PaymentNumber,
            VisitId = payment.VisitId,
            CustomerId = payment.CustomerId,
            CustomerName = payment.Customer.Name,
            SalesRepId = payment.SalesRepId,
            SalesRepName = payment.SalesRep.FullName,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            Reference = payment.Reference,
            Notes = payment.Notes,
            ReviewedByUserId = payment.ReviewedByUserId,
            ReviewedByUserName = payment.ReviewedByUser?.FullName,
            ReviewedAtUtc = payment.ReviewedAtUtc,
            RejectionReason = payment.RejectionReason,
            RowVersion = RowVersionTokenHelper.Encode(payment.RowVersion),
            CreatedAtUtc = payment.CreatedAtUtc,
            UpdatedAtUtc = payment.UpdatedAtUtc
        };
    }
}