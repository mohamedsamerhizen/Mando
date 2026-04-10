using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Operations;
using Mando.Api.DTOs.Orders;
using Mando.Api.DTOs.Payments;
using Mando.Api.DTOs.Visits;
using Mando.Api.Entities;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Interfaces.Operations;
using Mando.Api.Interfaces.Orders;
using Mando.Api.Interfaces.Payments;
using Mando.Api.Interfaces.Visits;
using Mando.Api.Models.Operations;

namespace Mando.Api.Services.Operations;

public class OperationsQueryService : IOperationsQueryService
{
    private const int MaxItemsLimit = 100;
    private const int MinItemsLimit = 1;
    private const int DefaultTopCount = 5;
    private const int MaxTopCount = 20;
    private const decimal HighBalanceImpactRatioThreshold = 0.80m;

    private readonly AppDbContext _context;
    private readonly ICustomerBalanceService _customerBalanceService;
    private readonly IPaymentQueryService _paymentQueryService;
    private readonly IOrderQueryService _orderQueryService;
    private readonly IVisitQueryService _visitQueryService;

    public OperationsQueryService(
        AppDbContext context,
        ICustomerBalanceService customerBalanceService,
        IPaymentQueryService paymentQueryService,
        IOrderQueryService orderQueryService,
        IVisitQueryService visitQueryService)
    {
        _context = context;
        _customerBalanceService = customerBalanceService;
        _paymentQueryService = paymentQueryService;
        _orderQueryService = orderQueryService;
        _visitQueryService = visitQueryService;
    }

    public async Task<OperationsQueryResult<OperationsDashboardResponseDto>> GetTodayDashboardAsync(
        Guid? salesRepId,
        Guid? customerId,
        VisitStatus? visitStatus,
        PaymentStatus? paymentStatus,
        bool includeVisits,
        bool includeOrders,
        bool includePayments,
        int itemsLimit)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = new GetOperationsDashboardQueryDto
        {
            DateFromUtc = today,
            DateToUtc = tomorrow,
            SalesRepId = salesRepId,
            CustomerId = customerId,
            VisitStatus = visitStatus,
            PaymentStatus = paymentStatus,
            IncludeVisits = includeVisits,
            IncludeOrders = includeOrders,
            IncludePayments = includePayments,
            ItemsLimit = itemsLimit
        };

        var validationError = ValidateDashboardQuery(query);
        if (validationError is not null)
        {
            return new OperationsQueryResult<OperationsDashboardResponseDto>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = validationError
            };
        }

        var response = await BuildDashboardAsync(query);

        return new OperationsQueryResult<OperationsDashboardResponseDto>
        {
            Status = OperationsQueryStatus.Success,
            Data = response
        };
    }

    public async Task<OperationsQueryResult<OperationsDashboardResponseDto>> GetRangeDashboardAsync(
        GetOperationsDashboardQueryDto query)
    {
        var validationError = ValidateDashboardQuery(query, requireDates: true);
        if (validationError is not null)
        {
            return new OperationsQueryResult<OperationsDashboardResponseDto>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = validationError
            };
        }

        var normalizedQuery = NormalizeDashboardQuery(query);
        var response = await BuildDashboardAsync(normalizedQuery);

        return new OperationsQueryResult<OperationsDashboardResponseDto>
        {
            Status = OperationsQueryStatus.Success,
            Data = response
        };
    }

    public async Task<OperationsQueryResult<OperationsKpiDashboardResponseDto>> GetTodayKpisAsync(
        int topCount)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var query = new GetOperationsKpiQueryDto
        {
            DateFromUtc = today,
            DateToUtc = tomorrow,
            TopCount = topCount
        };

        var validationError = ValidateKpiQuery(query);
        if (validationError is not null)
        {
            return new OperationsQueryResult<OperationsKpiDashboardResponseDto>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = validationError
            };
        }

        var response = await BuildKpiDashboardAsync(query);

        return new OperationsQueryResult<OperationsKpiDashboardResponseDto>
        {
            Status = OperationsQueryStatus.Success,
            Data = response
        };
    }

    public async Task<OperationsQueryResult<OperationsKpiDashboardResponseDto>> GetRangeKpisAsync(
        GetOperationsKpiQueryDto query)
    {
        var validationError = ValidateKpiQuery(query, requireDates: true);
        if (validationError is not null)
        {
            return new OperationsQueryResult<OperationsKpiDashboardResponseDto>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = validationError
            };
        }

        var normalizedQuery = NormalizeKpiQuery(query);
        var response = await BuildKpiDashboardAsync(normalizedQuery);

        return new OperationsQueryResult<OperationsKpiDashboardResponseDto>
        {
            Status = OperationsQueryStatus.Success,
            Data = response
        };
    }

    public async Task<OperationsQueryResult<UnifiedOperationsDashboardResponseDto>> GetUnifiedDashboardAsync(
        GetUnifiedOperationsDashboardQueryDto query)
    {
        var validationError = ValidateUnifiedDashboardQuery(query);
        if (validationError is not null)
        {
            return new OperationsQueryResult<UnifiedOperationsDashboardResponseDto>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = validationError
            };
        }

        var normalizedQuery = NormalizeUnifiedDashboardQuery(query);

        var paymentReportResult = await _paymentQueryService.GetOperationsReportAsync(
            new GetPaymentOperationsReportQueryDto
            {
                DateFromUtc = normalizedQuery.DateFromUtc,
                DateToUtc = normalizedQuery.DateToUtc,
                SalesRepId = normalizedQuery.SalesRepId,
                CustomerId = normalizedQuery.CustomerId,
                StaleAfterHours = normalizedQuery.PaymentStaleAfterHours
            });

        var orderReportResult = await _orderQueryService.GetOperationsReportAsync(
            new GetOrderOperationsReportQueryDto
            {
                DateFromUtc = normalizedQuery.DateFromUtc,                DateToUtc = normalizedQuery.DateToUtc,
                SalesRepId = normalizedQuery.SalesRepId,
                CustomerId = normalizedQuery.CustomerId,
                StaleAfterHours = normalizedQuery.OrderStaleAfterHours
            });

        var visitReportResult = await _visitQueryService.GetOperationsReportAsync(
            new GetVisitOperationsReportQueryDto
            {
                DateFromUtc = normalizedQuery.DateFromUtc,
                DateToUtc = normalizedQuery.DateToUtc,
                SalesRepId = normalizedQuery.SalesRepId,
                CustomerId = normalizedQuery.CustomerId,
                StaleAfterHours = normalizedQuery.VisitStaleAfterHours
            });

        if (paymentReportResult.Data is null || orderReportResult.Data is null || visitReportResult.Data is null)
        {
            return new OperationsQueryResult<UnifiedOperationsDashboardResponseDto>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = "Failed to build unified operations dashboard."
            };
        }

        var paymentReport = paymentReportResult.Data;
        var orderReport = orderReportResult.Data;
        var visitReport = visitReportResult.Data;

        var attentionSummary = new UnifiedOperationsAttentionSummaryDto
        {
            PendingPaymentsCount = paymentReport.BacklogSnapshot.PendingCount,
            StalePendingPaymentsCount = paymentReport.BacklogSnapshot.StalePendingCount,
            ApprovalBlockedPaymentsCount = paymentReport.BacklogSnapshot.ApprovalBlockedCount,
            PaymentsRequiringAttentionCount = paymentReport.BacklogSnapshot.AttentionRequiredCount,
            ActiveOrdersCount = orderReport.ActiveSnapshot.ActiveOrdersCount,
            StaleActiveOrdersCount = orderReport.ActiveSnapshot.StaleActiveOrdersCount,
            InProgressVisitsCount = visitReport.ActiveSnapshot.InProgressCount,
            StaleInProgressVisitsCount = visitReport.ActiveSnapshot.StaleInProgressCount
        };

        attentionSummary.TotalAttentionSignals =
            attentionSummary.PaymentsRequiringAttentionCount +
            attentionSummary.StaleActiveOrdersCount +
            attentionSummary.StaleInProgressVisitsCount;

        var flowSummary = new UnifiedOperationsFlowSummaryDto
        {
            StartedVisitsCount = visitReport.ThroughputSummary.StartedCount,
            CompletedVisitsCount = visitReport.ThroughputSummary.CompletedCount,
            CancelledVisitsCount = visitReport.ThroughputSummary.CancelledCount,
            SubmittedOrdersCount = orderReport.ThroughputSummary.SubmittedCount,
            CancelledOrdersCount = orderReport.ThroughputSummary.CancelledCount,
            SubmittedPaymentsCount = paymentReport.ThroughputSummary.SubmittedCount,
            ApprovedPaymentsCount = paymentReport.ThroughputSummary.ApprovedCount,
            RejectedPaymentsCount = paymentReport.ThroughputSummary.RejectedCount
        };

        return new OperationsQueryResult<UnifiedOperationsDashboardResponseDto>
        {
            Status = OperationsQueryStatus.Success,
            Data = new UnifiedOperationsDashboardResponseDto
            {
                GeneratedAtUtc = DateTime.UtcNow,
                RangeFromUtc = normalizedQuery.DateFromUtc!.Value,
                RangeToUtc = normalizedQuery.DateToUtc!.Value,
                AttentionSummary = attentionSummary,
                FlowSummary = flowSummary,
                Payments = paymentReport,
                Orders = orderReport,
                Visits = visitReport
            }
        };
    }

    public async Task<OperationsQueryResult<OperationsAlertsResponseDto>> GetAlertsAsync(
        GetOperationsAlertsQueryDto query)
    {
        var validationError = ValidateAlertsQuery(query);
        if (validationError is not null)
        {
            return new OperationsQueryResult<OperationsAlertsResponseDto>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = validationError
            };
        }

        var normalizedQuery = NormalizeAlertsQuery(query);
        var generatedAtUtc = DateTime.UtcNow;

        var alerts = new List<OperationsAlertItemDto>();

        alerts.AddRange(await BuildPaymentAlertsAsync(normalizedQuery, generatedAtUtc));
        alerts.AddRange(await BuildOrderAlertsAsync(normalizedQuery, generatedAtUtc));
        alerts.AddRange(await BuildVisitAlertsAsync(normalizedQuery, generatedAtUtc));
        alerts.AddRange(await BuildCustomerBalanceAlertsAsync(normalizedQuery, generatedAtUtc));

        await EnrichAlertsWithLatestReviewsAsync(alerts);

        var filteredAlerts = alerts.AsEnumerable();

        if (normalizedQuery.Severity.HasValue)
            filteredAlerts = filteredAlerts.Where(x => x.Severity == normalizedQuery.Severity.Value);

        if (normalizedQuery.Category.HasValue)
            filteredAlerts = filteredAlerts.Where(x => x.Category == normalizedQuery.Category.Value);

        if (normalizedQuery.EntityType.HasValue)
            filteredAlerts = filteredAlerts.Where(x => x.EntityType == normalizedQuery.EntityType.Value);

        var orderedAlerts = filteredAlerts
            .OrderByDescending(x => GetSeverityRank(x.Severity))
            .ThenByDescending(x => x.AgeInHours)
            .ThenBy(x => x.TriggeredAtUtc)
            .ToList();

        var pageNumber = normalizedQuery.PageNumber < 1 ? 1 : normalizedQuery.PageNumber;
        var pageSize = normalizedQuery.PageSize < 1 ? 20 : normalizedQuery.PageSize;
        if (pageSize > 200)
            pageSize = 200;

        var pagedAlerts = orderedAlerts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new OperationsQueryResult<OperationsAlertsResponseDto>
        {
            Status = OperationsQueryStatus.Success,
            Data = new OperationsAlertsResponseDto
            {
                GeneratedAtUtc = generatedAtUtc,
                PaymentStaleAfterHours = normalizedQuery.PaymentStaleAfterHours,
                OrderStaleAfterHours = normalizedQuery.OrderStaleAfterHours,
                VisitStaleAfterHours = normalizedQuery.VisitStaleAfterHours,
                NearCreditLimitRatio = normalizedQuery.NearCreditLimitRatio,
                Summary = BuildAlertSummary(orderedAlerts),
                Queue = new PagedResultDto<OperationsAlertItemDto>
                {
                    Items = pagedAlerts,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = orderedAlerts.Count
                }
            }
        };
    }

    public async Task<OperationsQueryResult<IReadOnlyList<OperationsAlertReviewDto>>> GetAlertReviewHistoryAsync(
        string alertFingerprint)
    {
        if (string.IsNullOrWhiteSpace(alertFingerprint))
        {
            return new OperationsQueryResult<IReadOnlyList<OperationsAlertReviewDto>>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = "AlertFingerprint is required."
            };
        }

        if (!OperationsAlertIdentityHelper.TryParseFingerprint(alertFingerprint, out _))
        {
            return new OperationsQueryResult<IReadOnlyList<OperationsAlertReviewDto>>
            {
                Status = OperationsQueryStatus.ValidationError,
                ValidationMessage = "AlertFingerprint is invalid."
            };
        }

        var reviews = await _context.OperationsAlertReviews
            .AsNoTracking()
            .Where(x => x.AlertFingerprint == alertFingerprint)
            .OrderByDescending(x => x.ReviewedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new OperationsAlertReviewDto
            {
                Id = x.Id,
                AlertKey = x.AlertKey,
                AlertFingerprint = x.AlertFingerprint,
                Category = x.Category,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                TriggeredAtUtc = x.TriggeredAtUtc,
                Status = x.Status,
                Comment = x.Comment,
                ReviewedByUserId = x.ReviewedByUserId,
                ReviewedByUserFullName = x.ReviewedByUserFullName,
                ReviewedAtUtc = x.ReviewedAtUtc
            })
            .ToListAsync();

        return new OperationsQueryResult<IReadOnlyList<OperationsAlertReviewDto>>
        {
            Status = OperationsQueryStatus.Success,
            Data = reviews
        };
    }

    private static string? ValidateDashboardQuery(GetOperationsDashboardQueryDto query, bool requireDates = false)
        {
        if (requireDates)
        {
            if (!query.DateFromUtc.HasValue)
                return "DateFromUtc is required.";

            if (!query.DateToUtc.HasValue)
                return "DateToUtc is required.";

            if (query.DateToUtc.Value < query.DateFromUtc.Value)
                return "DateToUtc must be greater than or equal to DateFromUtc.";
        }

        if (query.ItemsLimit < MinItemsLimit || query.ItemsLimit > MaxItemsLimit)
            return $"ItemsLimit must be between {MinItemsLimit} and {MaxItemsLimit}.";

        return null;
    }

    private static GetOperationsDashboardQueryDto NormalizeDashboardQuery(GetOperationsDashboardQueryDto query)
    {
        return new GetOperationsDashboardQueryDto
        {
            DateFromUtc = query.DateFromUtc!.Value,
            DateToUtc = query.DateToUtc!.Value == query.DateToUtc.Value.Date
                ? query.DateToUtc.Value.Date.AddDays(1)
                : query.DateToUtc.Value,
            SalesRepId = query.SalesRepId,
            CustomerId = query.CustomerId,
            VisitStatus = query.VisitStatus,
            PaymentStatus = query.PaymentStatus,
            IncludeVisits = query.IncludeVisits,
            IncludeOrders = query.IncludeOrders,
            IncludePayments = query.IncludePayments,
            ItemsLimit = query.ItemsLimit
        };
    }

    private static string? ValidateKpiQuery(GetOperationsKpiQueryDto query, bool requireDates = false)
    {
        if (requireDates)
        {
            if (!query.DateFromUtc.HasValue)
                return "DateFromUtc is required.";

            if (!query.DateToUtc.HasValue)
                return "DateToUtc is required.";

            if (query.DateToUtc.Value < query.DateFromUtc.Value)
                return "DateToUtc must be greater than or equal to DateFromUtc.";
        }

        if (query.TopCount < 1 || query.TopCount > MaxTopCount)
            return $"TopCount must be between 1 and {MaxTopCount}.";

        return null;
    }

    private static GetOperationsKpiQueryDto NormalizeKpiQuery(GetOperationsKpiQueryDto query)
    {
        return new GetOperationsKpiQueryDto
        {
            DateFromUtc = query.DateFromUtc!.Value,
            DateToUtc = query.DateToUtc!.Value == query.DateToUtc.Value.Date
                ? query.DateToUtc.Value.Date.AddDays(1)
                : query.DateToUtc.Value,
            TopCount = query.TopCount
        };
    }

    private static string? ValidateUnifiedDashboardQuery(GetUnifiedOperationsDashboardQueryDto query)
    {
        if (query.DateFromUtc.HasValue && query.DateToUtc.HasValue && query.DateToUtc.Value < query.DateFromUtc.Value)
            return "DateToUtc must be greater than or equal to DateFromUtc.";

        if (query.PaymentStaleAfterHours < 1 || query.PaymentStaleAfterHours > 24 * 30)
            return "PaymentStaleAfterHours must be between 1 and 720.";

        if (query.OrderStaleAfterHours < 1 || query.OrderStaleAfterHours > 24 * 30)
            return "OrderStaleAfterHours must be between 1 and 720.";

        if (query.VisitStaleAfterHours < 1 || query.VisitStaleAfterHours > 24 * 14)
            return "VisitStaleAfterHours must be between 1 and 336.";

        return null;
    }

    private static GetUnifiedOperationsDashboardQueryDto NormalizeUnifiedDashboardQuery(
        GetUnifiedOperationsDashboardQueryDto query)
    {
        var toUtc = query.DateToUtc ?? DateTime.UtcNow;
        if (toUtc.TimeOfDay == TimeSpan.Zero)
            toUtc = toUtc.Date.AddDays(1);

        var fromUtc = query.DateFromUtc ?? toUtc.AddDays(-7);

        return new GetUnifiedOperationsDashboardQueryDto
        {
            DateFromUtc = fromUtc,
            DateToUtc = toUtc,
            SalesRepId = query.SalesRepId,
            CustomerId = query.CustomerId,
            PaymentStaleAfterHours = query.PaymentStaleAfterHours,
            OrderStaleAfterHours = query.OrderStaleAfterHours,
            VisitStaleAfterHours = query.VisitStaleAfterHours
        };
    }

    private static string? ValidateAlertsQuery(GetOperationsAlertsQueryDto query)
    {
        if (query.PaymentStaleAfterHours < 1 || query.PaymentStaleAfterHours > 24 * 30)
            return "PaymentStaleAfterHours must be between 1 and 720.";

        if (query.OrderStaleAfterHours < 1 || query.OrderStaleAfterHours > 24 * 30)
            return "OrderStaleAfterHours must be between 1 and 720.";

        if (query.VisitStaleAfterHours < 1 || query.VisitStaleAfterHours > 24 * 14)
            return "VisitStaleAfterHours must be between 1 and 336.";

        if (query.NearCreditLimitRatio < 0.50m || query.NearCreditLimitRatio > 1.00m)
            return "NearCreditLimitRatio must be between 0.50 and 1.00.";

        return null;
    }

    private static GetOperationsAlertsQueryDto NormalizeAlertsQuery(GetOperationsAlertsQueryDto query)
    {
        return new GetOperationsAlertsQueryDto
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            SalesRepId = query.SalesRepId,
            CustomerId = query.CustomerId,
            Severity = query.Severity,
            Category = query.Category,
            EntityType = query.EntityType,
            PaymentStaleAfterHours = query.PaymentStaleAfterHours,
            OrderStaleAfterHours = query.OrderStaleAfterHours,
            VisitStaleAfterHours = query.VisitStaleAfterHours,
            NearCreditLimitRatio = query.NearCreditLimitRatio,
            IncludeNearCreditLimitAlerts = query.IncludeNearCreditLimitAlerts
        };
    }

    private async Task<List<OperationsAlertItemDto>> BuildPaymentAlertsAsync(
        GetOperationsAlertsQueryDto query,
        DateTime now)
    {
        var pendingPaymentsQuery = _context.Payments
            .AsNoTracking()
            .Where(x => x.Status == PaymentStatus.Pending)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            pendingPaymentsQuery = pendingPaymentsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        var pendingPayments = await pendingPaymentsQuery
            .Select(x => new PendingPaymentAlertRow
            {
                PaymentId = x.Id,
                PaymentNumber = x.PaymentNumber,
                VisitId = x.VisitId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Reference = x.Reference,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        if (pendingPayments.Count == 0)
            return [];

        var customerIds = pendingPayments
            .Select(x => x.CustomerId)
            .Distinct()
            .ToList();

        var balanceLookup = await BuildCustomerBalanceLookupAsync(customerIds);

        var pendingCountsByCustomerId = pendingPayments
            .GroupBy(x => x.CustomerId)
            .ToDictionary(g => g.Key, g => g.Count());

        var duplicateReferenceGroups = pendingPayments
            .Where(x => !string.IsNullOrWhiteSpace(x.Reference))
            .GroupBy(x => new { x.CustomerId, Reference = NormalizeReference(x.Reference!) })
            .Where(g => g.Count() > 1)
            .ToList();

        var alerts = new List<OperationsAlertItemDto>();

        foreach (var payment in pendingPayments)
        { var balanceSnapshot = balanceLookup.GetValueOrDefault(payment.CustomerId);
            var currentBalance = balanceSnapshot?.CurrentBalance ?? 0m;
            var ageInHours = Math.Round((now - payment.CreatedAtUtc).TotalHours, 2);

            if (ageInHours >= query.PaymentStaleAfterHours)
            {
                alerts.Add(CreateAlert(
                    severity: ResolveAgingSeverity(ageInHours, query.PaymentStaleAfterHours, criticalMultiplier: 3),
                    category: OperationsAlertCategory.PaymentStalePending,
                    entityType: OperationsAlertEntityType.Payment,
                    entityId: payment.PaymentId,
                    triggeredAtUtc: payment.CreatedAtUtc,
                    entityNumber: payment.PaymentNumber,
                    customerId: payment.CustomerId,
                    customerName: payment.CustomerName,
                    salesRepId: payment.SalesRepId,
                    salesRepName: payment.SalesRepName,
                    visitId: payment.VisitId,
                    shortReason: $"Pending payment has been waiting {ageInHours:0.##}h without a decision.",
                    recommendedAction: "Review and approve or reject this payment now so the queue does not age further.",
                    ageInHours: ageInHours,
                    amount: payment.Amount,
                    currentBalance: currentBalance,
                    creditLimit: balanceSnapshot?.CreditLimit,
                    balanceRatio: BuildCreditLimitRatio(balanceSnapshot),
                    reference: payment.Reference));
            }

            if (currentBalance <= 0 || payment.Amount > currentBalance)
            {
                alerts.Add(CreateAlert(
                    severity: OperationsAlertSeverity.Critical,
                    category: OperationsAlertCategory.PaymentApprovalBlocked,
                    entityType: OperationsAlertEntityType.Payment,
                    entityId: payment.PaymentId,
                    triggeredAtUtc: payment.CreatedAtUtc,
                    entityNumber: payment.PaymentNumber,
                    customerId: payment.CustomerId,
                    customerName: payment.CustomerName,
                    salesRepId: payment.SalesRepId,
                    salesRepName: payment.SalesRepName,
                    visitId: payment.VisitId,
                    shortReason: currentBalance <= 0
                        ? "Payment cannot be approved because the customer has no outstanding balance."
                        : $"Payment amount exceeds current outstanding balance ({currentBalance:0.00}).",
                    recommendedAction: "Reconcile customer balance before approving this payment. Reject it if the financial state does not justify the collection.",
                    ageInHours: ageInHours,
                    amount: payment.Amount,
                    currentBalance: currentBalance,
                    creditLimit: balanceSnapshot?.CreditLimit,
                    balanceRatio: BuildCreditLimitRatio(balanceSnapshot),
                    reference: payment.Reference));
            }

            if (payment.PaymentMethod != PaymentMethod.Cash && string.IsNullOrWhiteSpace(payment.Reference))
            {
                alerts.Add(CreateAlert(
                    severity: OperationsAlertSeverity.High,
                    category: OperationsAlertCategory.PaymentMissingReference,
                    entityType: OperationsAlertEntityType.Payment,
                    entityId: payment.PaymentId,
                    triggeredAtUtc: payment.CreatedAtUtc,
                    entityNumber: payment.PaymentNumber,
                    customerId: payment.CustomerId,
                    customerName: payment.CustomerName,
                    salesRepId: payment.SalesRepId,
                    salesRepName: payment.SalesRepName,
                    visitId: payment.VisitId,
                    shortReason: "Non-cash payment is missing a reference.",
                    recommendedAction: "Collect and verify the transfer or electronic payment reference before approval.",
                    ageInHours: ageInHours,
                    amount: payment.Amount,
                    currentBalance: currentBalance,
                    creditLimit: balanceSnapshot?.CreditLimit,
                    balanceRatio: BuildCreditLimitRatio(balanceSnapshot)));
            }

            if (currentBalance > 0 && payment.Amount / currentBalance >= HighBalanceImpactRatioThreshold)
            {
                alerts.Add(CreateAlert(
                    severity: OperationsAlertSeverity.Medium,
                    category: OperationsAlertCategory.PaymentHighBalanceImpact,
                    entityType: OperationsAlertEntityType.Payment,
                    entityId: payment.PaymentId,
                    triggeredAtUtc: payment.CreatedAtUtc,
                    entityNumber: payment.PaymentNumber,
                    customerId: payment.CustomerId,
                    customerName: payment.CustomerName,
                    salesRepId: payment.SalesRepId,
                    salesRepName: payment.SalesRepName,
                    visitId: payment.VisitId,
                    shortReason: "Payment amount covers a very large portion of the customer balance.",
                    recommendedAction: "Double-check the amount, reference, and collection context before approval.",
                    ageInHours: ageInHours,
                    amount: payment.Amount,
                    currentBalance: currentBalance,
                    creditLimit: balanceSnapshot?.CreditLimit,
                    balanceRatio: BuildCreditLimitRatio(balanceSnapshot),
                    reference: payment.Reference));
            }
        }

        foreach (var group in duplicateReferenceGroups)
        {
            var oldestPayment = group
                .OrderBy(x => x.CreatedAtUtc)
                .First();

            var balanceSnapshot = balanceLookup.GetValueOrDefault(oldestPayment.CustomerId);
            var ageInHours = Math.Round((now - oldestPayment.CreatedAtUtc).TotalHours, 2);

            alerts.Add(CreateAlert(
                severity: OperationsAlertSeverity.High,
                category: OperationsAlertCategory.PaymentDuplicateReference,
                entityType: OperationsAlertEntityType.Customer,
                entityId: oldestPayment.CustomerId,
                triggeredAtUtc: oldestPayment.CreatedAtUtc,
                qualifier: group.Key.Reference,
                entityNumber: group.Key.Reference,
                customerId: oldestPayment.CustomerId,
                customerName: oldestPayment.CustomerName,
                salesRepId: oldestPayment.SalesRepId,
                salesRepName: oldestPayment.SalesRepName,
                shortReason: $"Reference '{group.Key.Reference}' appears on {group.Count()} pending payments for the same customer.",
                recommendedAction: "Review the full duplicate-reference cluster before approving any payment in it.",
                ageInHours: ageInHours,
                amount: group.Sum(x => x.Amount),
                currentBalance: balanceSnapshot?.CurrentBalance,
                creditLimit: balanceSnapshot?.CreditLimit,
                balanceRatio: BuildCreditLimitRatio(balanceSnapshot),
                reference: group.Key.Reference,
                relatedCount: group.Count()));
        }

        foreach (var customerGroup in pendingPayments.GroupBy(x => x.CustomerId).Where(g => g.Count() > 1))
        {
            var oldestPayment = customerGroup
                .OrderBy(x => x.CreatedAtUtc)
                .First();

            var balanceSnapshot = balanceLookup.GetValueOrDefault(customerGroup.Key);
            var ageInHours = Math.Round((now - oldestPayment.CreatedAtUtc).TotalHours, 2);

            alerts.Add(CreateAlert(
                severity: OperationsAlertSeverity.Medium,
                category: OperationsAlertCategory.PaymentMultiplePending,
                entityType: OperationsAlertEntityType.Customer,
                entityId: customerGroup.Key,
                triggeredAtUtc: oldestPayment.CreatedAtUtc,
                customerId: customerGroup.Key,
                customerName: oldestPayment.CustomerName,
                salesRepId: oldestPayment.SalesRepId,
                salesRepName: oldestPayment.SalesRepName,
                shortReason: $"Customer has {pendingCountsByCustomerId[customerGroup.Key]} pending payments waiting for review.",
                recommendedAction: "Review this customer's full pending payment stack together to avoid fragmented decisions.",
                ageInHours: ageInHours,
                amount: customerGroup.Sum(x => x.Amount),
                currentBalance: balanceSnapshot?.CurrentBalance,
                creditLimit: balanceSnapshot?.CreditLimit,
                balanceRatio: BuildCreditLimitRatio(balanceSnapshot),
                relatedCount: pendingCountsByCustomerId[customerGroup.Key]));
        }

        return alerts;
    }

    private async Task<List<OperationsAlertItemDto>> BuildOrderAlertsAsync(
        GetOperationsAlertsQueryDto query,
        DateTime now)
    {
        var staleOrdersQuery = _context.Orders
            .AsNoTracking()
            .Where(x => x.Status != OrderStatus.Cancelled)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            staleOrdersQuery = staleOrdersQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            staleOrdersQuery = staleOrdersQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        var staleCutoffUtc = now.AddHours(-query.OrderStaleAfterHours);
        staleOrdersQuery = staleOrdersQuery.Where(x => x.CreatedAtUtc <= staleCutoffUtc);

        var staleOrders = await staleOrdersQuery
            .Select(x => new OrderAlertRow
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                VisitId = x.VisitId,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                TotalAmount = x.TotalAmount,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync();

        return staleOrders            .Select(order =>
            {
                var ageInHours = Math.Round((now - order.CreatedAtUtc).TotalHours, 2);

                return CreateAlert(
                    severity: ResolveAgingSeverity(ageInHours, query.OrderStaleAfterHours, criticalMultiplier: 3),
                    category: OperationsAlertCategory.OrderStaleActive,
                    entityType: OperationsAlertEntityType.Order,
                    entityId: order.OrderId,
                    triggeredAtUtc: order.CreatedAtUtc,
                    entityNumber: order.OrderNumber,
                    customerId: order.CustomerId,
                    customerName: order.CustomerName,
                    salesRepId: order.SalesRepId,
                    salesRepName: order.SalesRepName,
                    visitId: order.VisitId,
                    shortReason: $"Active order has remained open for {ageInHours:0.##}h.",
                    recommendedAction: "Confirm whether this order should remain active or be administratively cancelled.",
                    ageInHours: ageInHours,
                    amount: order.TotalAmount);
            })
            .ToList();
    }

    private async Task<List<OperationsAlertItemDto>> BuildVisitAlertsAsync(
        GetOperationsAlertsQueryDto query,
        DateTime now)
    {
        var staleVisitsQuery = _context.Visits
            .AsNoTracking()
            .Where(x => x.Status == VisitStatus.InProgress)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            staleVisitsQuery = staleVisitsQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            staleVisitsQuery = staleVisitsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        var staleCutoffUtc = now.AddHours(-query.VisitStaleAfterHours);
        staleVisitsQuery = staleVisitsQuery.Where(x => x.CheckInAtUtc <= staleCutoffUtc);

        var staleVisits = await staleVisitsQuery
            .Select(x => new VisitAlertRow
            {
                VisitId = x.Id,
                CustomerId = x.CustomerId,
                CustomerName = x.Customer.Name,
                SalesRepId = x.SalesRepId,
                SalesRepName = x.SalesRep.FullName,
                CheckInAtUtc = x.CheckInAtUtc
            })
            .ToListAsync();

        return staleVisits
            .Select(visit =>
            {
                var ageInHours = Math.Round((now - visit.CheckInAtUtc).TotalHours, 2);

                return CreateAlert(
                    severity: ResolveAgingSeverity(ageInHours, query.VisitStaleAfterHours, criticalMultiplier: 2),
                    category: OperationsAlertCategory.VisitStaleInProgress,
                    entityType: OperationsAlertEntityType.Visit,
                    entityId: visit.VisitId,
                    triggeredAtUtc: visit.CheckInAtUtc,
                    customerId: visit.CustomerId,
                    customerName: visit.CustomerName,
                    salesRepId: visit.SalesRepId,
                    salesRepName: visit.SalesRepName,
                    shortReason: $"Visit has been in progress for {ageInHours:0.##}h without closure.",
                    recommendedAction: "Contact the sales rep and resolve the visit state before more workflow drift accumulates.",
                    ageInHours: ageInHours);
            })
            .ToList();
    }

    private async Task<List<OperationsAlertItemDto>> BuildCustomerBalanceAlertsAsync(
        GetOperationsAlertsQueryDto query,
        DateTime now)
    {
        var customersQuery = _context.Customers
            .AsNoTracking()
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            customersQuery = customersQuery.Where(x => x.AssignedSalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            customersQuery = customersQuery.Where(x => x.Id == query.CustomerId.Value);

        var customerIds = await customersQuery
            .Select(x => x.Id)
            .ToListAsync();

        if (customerIds.Count == 0)
            return [];

        var balanceLookup = await BuildCustomerBalanceLookupAsync(customerIds);
        var alerts = new List<OperationsAlertItemDto>();

        foreach (var balanceSnapshot in balanceLookup.Values)
        {
            if (balanceSnapshot.CurrentBalance <= 0)
                continue;

            var balanceRatio = BuildCreditLimitRatio(balanceSnapshot);
            var triggeredAtUtc = balanceSnapshot.LastExposureAtUtc ?? now;
            var ageInHours = Math.Round((now - triggeredAtUtc).TotalHours, 2);

            var hasCreditLimit = balanceSnapshot.CreditLimit > 0;
            var isOverCreditLimit = !hasCreditLimit
                ? balanceSnapshot.CurrentBalance > 0
                : balanceSnapshot.CurrentBalance > balanceSnapshot.CreditLimit;

            if (isOverCreditLimit)
            {
                alerts.Add(CreateAlert(
                    severity: OperationsAlertSeverity.Critical,
                    category: OperationsAlertCategory.CustomerOverCreditLimit,
                    entityType: OperationsAlertEntityType.Customer,
                    entityId: balanceSnapshot.CustomerId,
                    triggeredAtUtc: triggeredAtUtc,
                    customerId: balanceSnapshot.CustomerId,
                    customerName: balanceSnapshot.CustomerName,
                    salesRepId: balanceSnapshot.SalesRepId,
                    salesRepName: balanceSnapshot.SalesRepName,
                    shortReason: hasCreditLimit
                        ? $"Customer balance ({balanceSnapshot.CurrentBalance:0.00}) is above credit limit ({balanceSnapshot.CreditLimit:0.00})."
                        : $"Customer balance ({balanceSnapshot.CurrentBalance:0.00}) exceeds a zero-credit account.",
                    recommendedAction: "Review collections, credit policy, and open exposure on this customer before allowing more risk.",
                    ageInHours: ageInHours,
                    currentBalance: balanceSnapshot.CurrentBalance,
                    creditLimit: balanceSnapshot.CreditLimit,
                    balanceRatio: balanceRatio));

                continue;
            }

            if (query.IncludeNearCreditLimitAlerts &&
                hasCreditLimit &&
                balanceRatio.HasValue &&
                balanceRatio.Value >= query.NearCreditLimitRatio)
            {
                alerts.Add(CreateAlert(
                    severity: OperationsAlertSeverity.High,
                    category: OperationsAlertCategory.CustomerNearCreditLimit,
                    entityType: OperationsAlertEntityType.Customer,
                    entityId: balanceSnapshot.CustomerId,
                    triggeredAtUtc: triggeredAtUtc,
                    customerId: balanceSnapshot.CustomerId,
                    customerName: balanceSnapshot.CustomerName,
                    salesRepId: balanceSnapshot.SalesRepId,
                    salesRepName: balanceSnapshot.SalesRepName,
                    shortReason: $"Customer balance has reached {(balanceRatio.Value * 100m):0.##}% of the credit limit.",
                    recommendedAction: "Prioritize collection planning and watch new order exposure on this customer closely.",
                    ageInHours: ageInHours,
                    currentBalance: balanceSnapshot.CurrentBalance,
                    creditLimit: balanceSnapshot.CreditLimit,
                    balanceRatio: balanceRatio));
            }
        }

        return alerts;
    }

    private async Task EnrichAlertsWithLatestReviewsAsync(List<OperationsAlertItemDto> alerts)
    {
        if (alerts.Count == 0)
            return;

        var fingerprints = alerts
            .Select(x => x.AlertFingerprint)
            .Distinct()
            .ToList();

        var latestReviews = await _context.OperationsAlertReviews
            .AsNoTracking()
            .Where(x => fingerprints.Contains(x.AlertFingerprint))
            .OrderByDescending(x => x.ReviewedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync();

        var latestReviewsByFingerprint = latestReviews
            .GroupBy(x => x.AlertFingerprint)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var alert in alerts)
        {
            if (!latestReviewsByFingerprint.TryGetValue(alert.AlertFingerprint, out var latestReview))
                continue;

            alert.ReviewStatus = latestReview.Status;
            alert.LatestReview = new OperationsAlertReviewDto
            {
                Id = latestReview.Id,
                AlertKey = latestReview.AlertKey,
                AlertFingerprint = latestReview.AlertFingerprint,
                Category = latestReview.Category,
                EntityType = latestReview.EntityType,
                EntityId = latestReview.EntityId,
                                TriggeredAtUtc = latestReview.TriggeredAtUtc,
                Status = latestReview.Status,
                Comment = latestReview.Comment,
                ReviewedByUserId = latestReview.ReviewedByUserId,
                ReviewedByUserFullName = latestReview.ReviewedByUserFullName,
                ReviewedAtUtc = latestReview.ReviewedAtUtc
            };
        }
    }

    private async Task<Dictionary<Guid, CustomerBalanceAlertRow>> BuildCustomerBalanceLookupAsync(
        IReadOnlyCollection<Guid> customerIds)
    {
        if (customerIds.Count == 0)
            return new Dictionary<Guid, CustomerBalanceAlertRow>();

        var distinctCustomerIds = customerIds
            .Distinct()
            .ToList();

        var customers = await _context.Customers
            .Where(x => distinctCustomerIds.Contains(x.Id))
            .Select(x => new CustomerBalanceAlertRow
            {
                CustomerId = x.Id,
                CustomerName = x.Name,
                SalesRepId = x.AssignedSalesRepId,
                SalesRepName = x.AssignedSalesRep.FullName,
                LastExposureAtUtc = x.UpdatedAtUtc ?? x.CreatedAtUtc
            })
            .ToListAsync();

        if (customers.Count == 0)
            return new Dictionary<Guid, CustomerBalanceAlertRow>();

        var balanceSnapshots = await _customerBalanceService.GetSnapshotsAsync(distinctCustomerIds);

        var orderExposureTimestamps = await _context.Orders
            .Where(x => distinctCustomerIds.Contains(x.CustomerId) && x.Status != OrderStatus.Cancelled)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                LastOrderAtUtc = g.Max(x => x.CreatedAtUtc)
            })
            .ToDictionaryAsync(x => x.CustomerId, x => (DateTime?)x.LastOrderAtUtc);

        var approvedPaymentExposureTimestamps = await _context.Payments
            .Where(x => distinctCustomerIds.Contains(x.CustomerId) && x.Status == PaymentStatus.Approved)
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                LastApprovedPaymentAtUtc = g.Max(x => x.ReviewedAtUtc ?? x.CreatedAtUtc)
            })
            .ToDictionaryAsync(x => x.CustomerId, x => (DateTime?)x.LastApprovedPaymentAtUtc);

        foreach (var customer in customers)
        {
            if (balanceSnapshots.TryGetValue(customer.CustomerId, out var balanceSnapshot))
            {
                customer.CurrentBalance = balanceSnapshot.CurrentBalance;
                customer.CreditLimit = balanceSnapshot.CreditLimit;
            }

            orderExposureTimestamps.TryGetValue(customer.CustomerId, out var lastOrderAtUtc);
            approvedPaymentExposureTimestamps.TryGetValue(customer.CustomerId, out var lastApprovedPaymentAtUtc);

            customer.LastExposureAtUtc = new[]
                {
                    customer.LastExposureAtUtc,
                    lastOrderAtUtc,
                    lastApprovedPaymentAtUtc
                }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty(customer.LastExposureAtUtc ?? DateTime.UtcNow)
                .Max();
        }

        return customers.ToDictionary(x => x.CustomerId, x => x);
    }

    private static OperationsAlertsSummaryDto BuildAlertSummary(IReadOnlyCollection<OperationsAlertItemDto> alerts)
    {
        return new OperationsAlertsSummaryDto
        {
            TotalCount = alerts.Count,
            CriticalCount = alerts.Count(x => x.Severity == OperationsAlertSeverity.Critical),
            HighCount = alerts.Count(x => x.Severity == OperationsAlertSeverity.High),
            MediumCount = alerts.Count(x => x.Severity == OperationsAlertSeverity.Medium),
            PaymentAlertsCount = alerts.Count(x => x.EntityType == OperationsAlertEntityType.Payment),
            OrderAlertsCount = alerts.Count(x => x.EntityType == OperationsAlertEntityType.Order),
            VisitAlertsCount = alerts.Count(x => x.EntityType == OperationsAlertEntityType.Visit),
            CustomerAlertsCount = alerts.Count(x => x.EntityType == OperationsAlertEntityType.Customer),
            OpenCount = alerts.Count(x => x.ReviewStatus == OperationsAlertReviewStatus.Open),
            AcknowledgedCount = alerts.Count(x => x.ReviewStatus == OperationsAlertReviewStatus.Acknowledged),
            ResolvedCount = alerts.Count(x => x.ReviewStatus == OperationsAlertReviewStatus.Resolved),
            DismissedCount = alerts.Count(x => x.ReviewStatus == OperationsAlertReviewStatus.Dismissed)
        };
    }

    private static OperationsAlertItemDto CreateAlert(
        OperationsAlertSeverity severity,
        OperationsAlertCategory category,
        OperationsAlertEntityType entityType,
        Guid entityId,
        DateTime triggeredAtUtc,
        string shortReason,
        string recommendedAction,
        double ageInHours,
        string? qualifier = null,
        string? entityNumber = null,
        Guid? customerId = null,
        string? customerName = null,
        Guid? salesRepId = null,
        string? salesRepName = null,
        Guid? visitId = null,
        decimal? amount = null,
        decimal? currentBalance = null,
        decimal? creditLimit = null,
        decimal? balanceRatio = null,
        string? reference = null,
        int? relatedCount = null)
    {
        var alertKey = OperationsAlertIdentityHelper.BuildAlertKey(category, entityType, entityId, qualifier);
        return new OperationsAlertItemDto
        {
            AlertKey = alertKey,
            AlertFingerprint = OperationsAlertIdentityHelper.BuildAlertFingerprint(alertKey, triggeredAtUtc),
            Severity = severity,
            Category = category,
            EntityType = entityType,
            EntityId = entityId,
            EntityNumber = entityNumber,
            CustomerId = customerId,
            CustomerName = customerName,
            SalesRepId = salesRepId,
            SalesRepName = salesRepName,
            VisitId = visitId,
            ShortReason = shortReason,
            RecommendedAction = recommendedAction,
            TriggeredAtUtc = triggeredAtUtc,
            AgeInHours = ageInHours,
            Amount = amount,
            CurrentBalance = currentBalance,
            CreditLimit = creditLimit,
            BalanceRatio = balanceRatio,
            Reference = reference,
            RelatedCount = relatedCount
        };
    }

    private static decimal? BuildCreditLimitRatio(CustomerBalanceAlertRow? balanceSnapshot)
    {
        if (balanceSnapshot is null || balanceSnapshot.CreditLimit <= 0)
            return null;

        return Math.Round(balanceSnapshot.CurrentBalance / balanceSnapshot.CreditLimit, 4);
    }

    private static OperationsAlertSeverity ResolveAgingSeverity(
        double ageInHours,
        int staleAfterHours,
        int criticalMultiplier)
    {
        return ageInHours >= staleAfterHours * criticalMultiplier
            ? OperationsAlertSeverity.Critical
            : OperationsAlertSeverity.High;
    }

    private static int GetSeverityRank(OperationsAlertSeverity severity)
    {
        return severity switch
        {
            OperationsAlertSeverity.Critical => 3,
            OperationsAlertSeverity.High => 2,
            _ => 1
        };
    }

    private static string NormalizeReference(string reference)
    {
        return OperationsAlertIdentityHelper.NormalizeQualifier(reference);
    }

    private sealed class PendingPaymentAlertRow
    {
        public Guid PaymentId { get; init; }
        public string PaymentNumber { get; init; } = string.Empty;
        public Guid VisitId { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public PaymentMethod PaymentMethod { get; init; }
        public string? Reference { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }    private sealed class OrderAlertRow
    {
        public Guid OrderId { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public Guid VisitId { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public DateTime CreatedAtUtc { get; init; }
    }

    private sealed class VisitAlertRow
    {
        public Guid VisitId { get; init; }
        public Guid CustomerId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public DateTime CheckInAtUtc { get; init; }
    }

    private sealed class CustomerBalanceAlertRow
    {
        public Guid CustomerId { get; init; }
        public string CustomerName { get; init; } = string.Empty;
        public Guid SalesRepId { get; init; }
        public string SalesRepName { get; init; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public DateTime? LastExposureAtUtc { get; set; }
    }

    private async Task<OperationsDashboardResponseDto> BuildDashboardAsync(GetOperationsDashboardQueryDto query)
    {
        var dateFromUtc = query.DateFromUtc!.Value;
        var dateToUtc = query.DateToUtc!.Value;

        var visitsQuery = _context.Visits
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Where(x => x.CheckInAtUtc >= dateFromUtc && x.CheckInAtUtc < dateToUtc)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            visitsQuery = visitsQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            visitsQuery = visitsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        if (query.VisitStatus.HasValue)
            visitsQuery = visitsQuery.Where(x => x.Status == query.VisitStatus.Value);

        var visitsSummary = await visitsQuery
            .Select(x => new
            {
                x.Status
            })
            .ToListAsync();

        var visits = query.IncludeVisits
            ? await visitsQuery
                .OrderByDescending(x => x.CheckInAtUtc)
                .Take(query.ItemsLimit)
                .Select(x => new OperationVisitSummaryDto
                {
                    VisitId = x.Id,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,
                    SalesRepId = x.SalesRepId,
                    SalesRepName = x.SalesRep.FullName,
                    CheckInAtUtc = x.CheckInAtUtc,
                    CheckOutAtUtc = x.CheckOutAtUtc,
                    Status = x.Status,
                    Outcome = x.Outcome
                })
                .ToListAsync()
            : [];

        var ordersQuery = _context.Orders
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Where(x => x.CreatedAtUtc >= dateFromUtc && x.CreatedAtUtc < dateToUtc)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            ordersQuery = ordersQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            ordersQuery = ordersQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        var ordersSummary = await ordersQuery
            .Select(x => new
            {
                x.TotalAmount
            })
            .ToListAsync();

        var orders = query.IncludeOrders
            ? await ordersQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(query.ItemsLimit)
                .Select(x => new OperationOrderSummaryDto
                {
                    OrderId = x.Id,
                    OrderNumber = x.OrderNumber,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,
                    SalesRepId = x.SalesRepId,
                    SalesRepName = x.SalesRep.FullName,
                    VisitId = x.VisitId,
                    TotalAmount = x.TotalAmount,
                    PaymentType = x.PaymentType,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToListAsync()
            : [];

        var paymentsQuery = _context.Payments
            .Include(x => x.Customer)
            .Include(x => x.SalesRep)
            .Include(x => x.ReviewedByUser)
            .Where(x => x.CreatedAtUtc >= dateFromUtc && x.CreatedAtUtc < dateToUtc)
            .AsQueryable();

        if (query.SalesRepId.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.SalesRepId == query.SalesRepId.Value);

        if (query.CustomerId.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.CustomerId == query.CustomerId.Value);

        if (query.PaymentStatus.HasValue)
            paymentsQuery = paymentsQuery.Where(x => x.Status == query.PaymentStatus.Value);

        var paymentsSummary = await paymentsQuery
            .Select(x => new
            {
                x.Status,
                x.Amount
            })
            .ToListAsync();

        var payments = query.IncludePayments
            ? await paymentsQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(query.ItemsLimit)
                .Select(x => new OperationPaymentSummaryDto
                {
                    PaymentId = x.Id,
                    PaymentNumber = x.PaymentNumber,
                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,
                    SalesRepId = x.SalesRepId,
                    SalesRepName = x.SalesRep.FullName,
                    VisitId = x.VisitId,
                    Amount = x.Amount,
                    PaymentMethod = x.PaymentMethod,
                    Status = x.Status,
                    Reference = x.Reference,
                    CreatedAtUtc = x.CreatedAtUtc,
                    ReviewedAtUtc = x.ReviewedAtUtc,
                    ReviewedByUserId = x.ReviewedByUserId,
                    ReviewedByUserName = x.ReviewedByUser != null ? x.ReviewedByUser.FullName : null,
                    RejectionReason = x.RejectionReason
                })
                .ToListAsync()
            : [];

        return new OperationsDashboardResponseDto
        {
            DateFromUtc = dateFromUtc,
            DateToUtc = dateToUtc,
            TotalVisits = visitsSummary.Count,
            CompletedVisits = visitsSummary.Count(x => x.Status == VisitStatus.Completed),
            InProgressVisits = visitsSummary.Count(x => x.Status == VisitStatus.InProgress),
            CancelledVisits = visitsSummary.Count(x => x.Status == VisitStatus.Cancelled),
            TotalOrders = ordersSummary.Count,
            TotalSalesAmount = ordersSummary.Sum(x => x.TotalAmount),
            TotalPayments = paymentsSummary.Count,
            PendingPayments = paymentsSummary.Count(x => x.Status == PaymentStatus.Pending),
            ApprovedPaymentsCount = paymentsSummary.Count(x => x.Status == PaymentStatus.Approved),
            RejectedPaymentsCount = paymentsSummary.Count(x => x.Status == PaymentStatus.Rejected),
            ApprovedPaymentsAmount = paymentsSummary
                .Where(x => x.Status == PaymentStatus.Approved)
                .Sum(x => x.Amount),
            Visits = visits,
            Orders = orders,
            Payments = payments
        };
    }

    private async Task<OperationsKpiDashboardResponseDto> BuildKpiDashboardAsync(GetOperationsKpiQueryDto query)
    {
        var dateFromUtc = query.DateFromUtc!.Value;
        var dateToUtc = query.DateToUtc!.Value;
        var topCount = query.TopCount;

        var topSalesRepsByVisits = await _context.Visits            .Include(x => x.SalesRep)
            .Where(x => x.CheckInAtUtc >= dateFromUtc && x.CheckInAtUtc < dateToUtc)
            .GroupBy(x => new { x.SalesRepId, x.SalesRep.FullName })
            .Select(g => new TopSalesRepByVisitsDto
            {
                SalesRepId = g.Key.SalesRepId,
                SalesRepName = g.Key.FullName,
                VisitsCount = g.Count(),
                CompletedVisitsCount = g.Count(x => x.Status == VisitStatus.Completed)
            })
            .OrderByDescending(x => x.VisitsCount)
            .ThenByDescending(x => x.CompletedVisitsCount)
            .Take(topCount)
            .ToListAsync();

        var topSalesRepsBySales = await _context.Orders
            .Include(x => x.SalesRep)
            .Where(x => x.CreatedAtUtc >= dateFromUtc && x.CreatedAtUtc < dateToUtc)
            .GroupBy(x => new { x.SalesRepId, x.SalesRep.FullName })
            .Select(g => new TopSalesRepBySalesDto
            {
                SalesRepId = g.Key.SalesRepId,
                SalesRepName = g.Key.FullName,
                OrdersCount = g.Count(),
                TotalSalesAmount = g.Sum(x => x.TotalAmount)
            })
            .OrderByDescending(x => x.TotalSalesAmount)
            .ThenByDescending(x => x.OrdersCount)
            .Take(topCount)
            .ToListAsync();

        var topSalesRepsByCollections = await _context.Payments
            .Include(x => x.SalesRep)
            .Where(x =>
                x.Status == PaymentStatus.Approved &&
                x.ReviewedAtUtc.HasValue &&
                x.ReviewedAtUtc.Value >= dateFromUtc &&
                x.ReviewedAtUtc.Value < dateToUtc)
            .GroupBy(x => new { x.SalesRepId, x.SalesRep.FullName })
            .Select(g => new TopSalesRepByCollectionsDto
            {
                SalesRepId = g.Key.SalesRepId,
                SalesRepName = g.Key.FullName,
                ApprovedPaymentsCount = g.Count(),
                ApprovedCollectionsAmount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(x => x.ApprovedCollectionsAmount)
            .ThenByDescending(x => x.ApprovedPaymentsCount)
            .Take(topCount)
            .ToListAsync();

        var visitsActivity = await _context.Visits
            .Include(x => x.Customer)
            .Where(x => x.CheckInAtUtc >= dateFromUtc && x.CheckInAtUtc < dateToUtc)
            .GroupBy(x => new { x.CustomerId, x.Customer.Name })
            .Select(g => new
            {
                g.Key.CustomerId,
                CustomerName = g.Key.Name,
                VisitsCount = g.Count()
            })
            .ToListAsync();

        var ordersActivity = await _context.Orders
            .Include(x => x.Customer)
            .Where(x => x.CreatedAtUtc >= dateFromUtc && x.CreatedAtUtc < dateToUtc)
            .GroupBy(x => new { x.CustomerId, x.Customer.Name })
            .Select(g => new
            {
                g.Key.CustomerId,
                CustomerName = g.Key.Name,
                OrdersCount = g.Count()
            })
            .ToListAsync();

        var paymentsActivity = await _context.Payments
            .Include(x => x.Customer)
            .Where(x => x.CreatedAtUtc >= dateFromUtc && x.CreatedAtUtc < dateToUtc)
            .GroupBy(x => new { x.CustomerId, x.Customer.Name })
            .Select(g => new
            {
                g.Key.CustomerId,
                CustomerName = g.Key.Name,
                PaymentsCount = g.Count()
            })
            .ToListAsync();

        var customerActivityMap = new Dictionary<Guid, TopCustomerActivityDto>();

        foreach (var item in visitsActivity)
        {
            customerActivityMap[item.CustomerId] = new TopCustomerActivityDto
            {
                CustomerId = item.CustomerId,
                CustomerName = item.CustomerName,
                VisitsCount = item.VisitsCount
            };
        }

        foreach (var item in ordersActivity)
        {
            if (!customerActivityMap.TryGetValue(item.CustomerId, out var dto))
            {
                dto = new TopCustomerActivityDto
                {
                    CustomerId = item.CustomerId,
                    CustomerName = item.CustomerName
                };

                customerActivityMap[item.CustomerId] = dto;
            }

            dto.OrdersCount = item.OrdersCount;
        }

        foreach (var item in paymentsActivity)
        {
            if (!customerActivityMap.TryGetValue(item.CustomerId, out var dto))
            {
                dto = new TopCustomerActivityDto
                {
                    CustomerId = item.CustomerId,
                    CustomerName = item.CustomerName
                };

                customerActivityMap[item.CustomerId] = dto;
            }

            dto.PaymentsCount = item.PaymentsCount;
        }

        foreach (var dto in customerActivityMap.Values)
        {
            dto.ActivityScore = dto.VisitsCount + dto.OrdersCount + dto.PaymentsCount;
        }

        var topCustomersByActivity = customerActivityMap.Values
            .OrderByDescending(x => x.ActivityScore)
            .ThenByDescending(x => x.VisitsCount)
            .ThenByDescending(x => x.OrdersCount)
            .Take(topCount)
            .ToList();

        var customerDebtMetadata = await _context.Customers
            .Select(x => new
            {
                x.Id,
                x.Name
            })
            .ToListAsync();

        var customerBalanceSnapshots = await _customerBalanceService.GetSnapshotsAsync(
            customerDebtMetadata.Select(x => x.Id).ToList());

        var topCustomersByDebt = customerDebtMetadata
            .Select(x =>
            {
                customerBalanceSnapshots.TryGetValue(x.Id, out var balanceSnapshot);

                return new TopCustomerDebtDto
                {
                    CustomerId = x.Id,
                    CustomerName = x.Name,
                    OpeningBalance = balanceSnapshot?.OpeningBalance ?? 0m,
                    TotalOrders = balanceSnapshot?.TotalOrders ?? 0m,
                    ApprovedPayments = balanceSnapshot?.ApprovedPayments ?? 0m,
                    CurrentBalance = balanceSnapshot?.CurrentBalance ?? 0m
                };
            })
            .Where(x => x.CurrentBalance > 0)
            .OrderByDescending(x => x.CurrentBalance)
            .Take(topCount)
            .ToList();

        return new OperationsKpiDashboardResponseDto
        {
            DateFromUtc = dateFromUtc,
            DateToUtc = dateToUtc,
            TopCount = topCount,
            TopSalesRepsByVisits = topSalesRepsByVisits,
            TopSalesRepsBySales = topSalesRepsBySales,
            TopSalesRepsByCollections = topSalesRepsByCollections,
            TopCustomersByActivity = topCustomersByActivity,
            TopCustomersByDebt = topCustomersByDebt
        };
    }
}
