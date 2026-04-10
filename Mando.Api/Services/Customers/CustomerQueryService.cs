using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Customers;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Customers;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Models.Customers;

namespace Mando.Api.Services.Customers;

public class CustomerQueryService : ICustomerQueryService
{
    private readonly AppDbContext _context;
    private readonly ICustomerBalanceService _customerBalanceService;

    public CustomerQueryService(
        AppDbContext context,
        ICustomerBalanceService customerBalanceService)
    {
        _context = context;
        _customerBalanceService = customerBalanceService;
    }

    public async Task<CustomerQueryResult<PagedResultDto<CustomerResponseDto>>> GetAllAsync(
        GetCustomersQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var customersQuery = _context.Customers
            .Include(x => x.AssignedSalesRep)
            .AsQueryable();

        if (!IsAdminOrManager(currentUserRoles))
        {
            customersQuery = customersQuery.Where(x => x.AssignedSalesRepId == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            customersQuery = customersQuery.Where(x =>
                x.Name.Contains(search) ||
                x.Code.Contains(search) ||
                (x.PhoneNumber != null && x.PhoneNumber.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            customersQuery = customersQuery.Where(x => x.City == city);
        }

        if (!string.IsNullOrWhiteSpace(query.Region))
        {
            var region = query.Region.Trim();
            customersQuery = customersQuery.Where(x => x.Region == region);
        }

        var result = await customersQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => MapCustomer(x, x.AssignedSalesRep.FullName))
            .AsNoTracking()
            .ToPagedResultAsync(query.PageNumber, query.PageSize);

        return new CustomerQueryResult<PagedResultDto<CustomerResponseDto>>
        {
            Status = CustomerQueryStatus.Success,
            Data = result
        };
    }

    public async Task<CustomerQueryResult<CustomerResponseDto>> GetByIdAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var customer = await _context.Customers
            .Include(x => x.AssignedSalesRep)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerQueryResult<CustomerResponseDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        if (!IsAdminOrManager(currentUserRoles) && customer.AssignedSalesRepId != currentUser.Id)
        {
            return new CustomerQueryResult<CustomerResponseDto>
            {
                Status = CustomerQueryStatus.Forbidden
            };
        }

        return new CustomerQueryResult<CustomerResponseDto>
        {
            Status = CustomerQueryStatus.Success,
            Data = MapCustomer(customer, customer.AssignedSalesRep.FullName)
        };
    }

    public async Task<CustomerQueryResult<CustomerBalanceDto>> GetBalanceAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerQueryResult<CustomerBalanceDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        if (!IsAdminOrManager(currentUserRoles) && customer.AssignedSalesRepId != currentUser.Id)
        {
            return new CustomerQueryResult<CustomerBalanceDto>
            {
                Status = CustomerQueryStatus.Forbidden
            };
        }

        var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(customerId);
        if (balanceSnapshot is null)
        {
            return new CustomerQueryResult<CustomerBalanceDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        return new CustomerQueryResult<CustomerBalanceDto>
        {
            Status = CustomerQueryStatus.Success,
            Data = new CustomerBalanceDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerCode = customer.Code,
                OpeningBalance = balanceSnapshot.OpeningBalance,
                TotalOrders = balanceSnapshot.TotalOrders,
                ApprovedPayments = balanceSnapshot.ApprovedPayments,
                CurrentBalance = balanceSnapshot.CurrentBalance
            }
        };
    }

    public async Task<CustomerQueryResult<CustomerStatementResponseDto>> GetStatementAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var customer = await _context.Customers
            .Include(x => x.AssignedSalesRep)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerQueryResult<CustomerStatementResponseDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        if (!IsAdminOrManager(currentUserRoles) && customer.AssignedSalesRepId != currentUser.Id)
        {
            return new CustomerQueryResult<CustomerStatementResponseDto>
            {
                Status = CustomerQueryStatus.Forbidden
            };
        }

        var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(customerId);
        if (balanceSnapshot is null)
        {
            return new CustomerQueryResult<CustomerStatementResponseDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        var ordersCount = await _context.Orders
            .CountAsync(x => x.CustomerId == customerId);

        var paymentsCount = await _context.Payments
            .CountAsync(x => x.CustomerId == customerId);

        var recentOrders = await _context.Orders
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .Select(x => new CustomerStatementOrderDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                VisitId = x.VisitId,
                TotalAmount = x.TotalAmount,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        var recentPayments = await _context.Payments
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .Select(x => new CustomerStatementPaymentDto
            {
                Id = x.Id,
                PaymentNumber = x.PaymentNumber,
                VisitId = x.VisitId,
                Amount = x.Amount,
                PaymentMethod = x.PaymentMethod,
                Status = x.Status,
                Reference = x.Reference,
                CreatedAtUtc = x.CreatedAtUtc,
                ReviewedAtUtc = x.ReviewedAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        return new CustomerQueryResult<CustomerStatementResponseDto>
        {
            Status = CustomerQueryStatus.Success,
            Data = new CustomerStatementResponseDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerCode = customer.Code,
                AssignedSalesRepId = customer.AssignedSalesRepId,
                AssignedSalesRepName = customer.AssignedSalesRep.FullName,
                OpeningBalance = balanceSnapshot.OpeningBalance,
                TotalOrders = balanceSnapshot.TotalOrders,
                ApprovedPayments = balanceSnapshot.ApprovedPayments,
                CurrentBalance = balanceSnapshot.CurrentBalance,
                OrdersCount = ordersCount,
                PaymentsCount = paymentsCount,
                RecentOrders = recentOrders,
                RecentPayments = recentPayments
            }
        };
    }

    public async Task<CustomerQueryResult<IReadOnlyList<CustomerActionHistoryResponseDto>>> GetHistoryAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerQueryResult<IReadOnlyList<CustomerActionHistoryResponseDto>>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        if (!IsAdminOrManager(currentUserRoles) && customer.AssignedSalesRepId != currentUser.Id)
        {
            return new CustomerQueryResult<IReadOnlyList<CustomerActionHistoryResponseDto>>
            {
                Status = CustomerQueryStatus.Forbidden
            };
        }

        var history = await _context.CustomerActionHistories
            .Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.ActionAtUtc)
            .Select(x => new CustomerActionHistoryResponseDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                ActionType = x.ActionType,
                PreviousName = x.PreviousName,
                NewName = x.NewName,
                PreviousCode = x.PreviousCode,
                NewCode = x.NewCode,
                PreviousStatus = x.PreviousStatus,
                NewStatus = x.NewStatus,
                PreviousAssignedSalesRepId = x.PreviousAssignedSalesRepId,
                PreviousAssignedSalesRepName = x.PreviousAssignedSalesRepName,
                NewAssignedSalesRepId = x.NewAssignedSalesRepId,
                NewAssignedSalesRepName = x.NewAssignedSalesRepName,
                PreviousCreditLimit = x.PreviousCreditLimit,
                NewCreditLimit = x.NewCreditLimit,
                PreviousOpeningBalance = x.PreviousOpeningBalance,
                NewOpeningBalance = x.NewOpeningBalance,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByUserName = x.PerformedByUserFullName,
                Comment = x.Comment,
                ActionAtUtc = x.ActionAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        return new CustomerQueryResult<IReadOnlyList<CustomerActionHistoryResponseDto>>
        {
            Status = CustomerQueryStatus.Success,
            Data = history
        };
    }

    public async Task<CustomerQueryResult<CustomerFinancialLedgerResponseDto>> GetFinancialLedgerAsync(
        Guid customerId,
        GetCustomerFinancialLedgerQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerQueryResult<CustomerFinancialLedgerResponseDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        if (!IsAdminOrManager(currentUserRoles) && customer.AssignedSalesRepId != currentUser.Id)
        {
            return new CustomerQueryResult<CustomerFinancialLedgerResponseDto>
            {
                Status = CustomerQueryStatus.Forbidden
            };
        }

        var take = query.Take <= 0 ? 200 : query.Take;
        var fromUtc = query.FromUtc;
        var toUtc = query.ToUtc;

        var rawEntries = new List<CustomerFinancialLedgerRawEntry>();

        var openingBalanceHistory = await _context.CustomerActionHistories
            .Where(x => x.CustomerId == customerId)
            .Where(x =>
                x.ActionType == CustomerActionType.Created ||
                x.ActionType == CustomerActionType.FinancialSettingsAdjusted ||
                (x.ActionType == CustomerActionType.Updated && x.PreviousOpeningBalance != x.NewOpeningBalance))
            .Select(x => new
            {
                x.Id,
                x.ActionType,
                x.PreviousOpeningBalance,
                x.NewOpeningBalance,
                x.PerformedByUserId,
                x.PerformedByUserFullName,
                x.Comment,
                x.ActionAtUtc
            })
            .AsNoTracking()
            .ToListAsync();

        foreach (var item in openingBalanceHistory)
        {
            if (item.ActionType == CustomerActionType.Created)
            {
                rawEntries.Add(new CustomerFinancialLedgerRawEntry
                {
                    OccurredAtUtc = item.ActionAtUtc,
                    EntryType = CustomerFinancialLedgerEntryType.OpeningBalanceApplied,
                    DeltaAmount = item.NewOpeningBalance,
                    Description = "Opening balance applied.",
                    EntityId = item.Id,
                    ActorUserId = item.PerformedByUserId,
                    ActorUserName = item.PerformedByUserFullName,
                    Comment = item.Comment
                });

                continue;
            }

            var previous = item.PreviousOpeningBalance ?? 0m;
            var delta = item.NewOpeningBalance - previous;

            if (delta == 0m)
                continue;

            rawEntries.Add(new CustomerFinancialLedgerRawEntry
            {
                OccurredAtUtc = item.ActionAtUtc,
                EntryType = CustomerFinancialLedgerEntryType.OpeningBalanceAdjusted,
                DeltaAmount = delta,
                Description = "Opening balance adjusted.",
                EntityId = item.Id,
                ActorUserId = item.PerformedByUserId,
                ActorUserName = item.PerformedByUserFullName,
                Comment = item.Comment
            });
        }

        var orderEntries = await _context.Orders
            .Where(x => x.CustomerId == customerId && x.Status != OrderStatus.Cancelled)
            .Select(x => new CustomerFinancialLedgerRawEntry
            {
                OccurredAtUtc = x.CreatedAtUtc,
                EntryType = CustomerFinancialLedgerEntryType.OrderBooked,
                EntityId = x.Id,
                VisitId = x.VisitId,
                ReferenceNumber = x.OrderNumber,
                Description = "Order booked.",
                DeltaAmount = x.TotalAmount,
                OrderStatus = x.Status
            })
            .AsNoTracking()
            .ToListAsync();

        rawEntries.AddRange(orderEntries);

        var paymentEntries = await _context.Payments
            .Where(x => x.CustomerId == customerId && x.Status == PaymentStatus.Approved)
            .Select(x => new CustomerFinancialLedgerRawEntry
            {
                OccurredAtUtc = x.ReviewedAtUtc ?? x.CreatedAtUtc,
                EntryType = CustomerFinancialLedgerEntryType.ApprovedPaymentApplied,
                EntityId = x.Id,
                VisitId = x.VisitId,
                ReferenceNumber = x.PaymentNumber,
                Description = "Approved payment applied.",
                DeltaAmount = -x.Amount,
                PaymentMethod = x.PaymentMethod,
                PaymentStatus = x.Status,
                Comment = x.Reference
            })
            .AsNoTracking()
            .ToListAsync();

        rawEntries.AddRange(paymentEntries);

        var orderedEntries = rawEntries
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.EntryType)
            .ThenBy(x => x.ReferenceNumber)
            .ToList();

        var openingBalanceAtRangeStart = fromUtc.HasValue
            ? orderedEntries
                .Where(x => x.OccurredAtUtc < fromUtc.Value)
                .Sum(x => x.DeltaAmount)
            : 0m;

        var entriesInRange = orderedEntries
            .Where(x => !fromUtc.HasValue || x.OccurredAtUtc >= fromUtc.Value)
            .Where(x => !toUtc.HasValue || x.OccurredAtUtc <= toUtc.Value)
            .ToList();

        var totalEntriesInRange = entriesInRange.Count;
        var returnedRawEntries = entriesInRange
            .Take(take)
            .ToList();

        var runningBalance = openingBalanceAtRangeStart;
        var returnedEntries = new List<CustomerFinancialLedgerEntryDto>(returnedRawEntries.Count);

        foreach (var entry in returnedRawEntries)
        {
            runningBalance += entry.DeltaAmount;

            returnedEntries.Add(new CustomerFinancialLedgerEntryDto
            {
                OccurredAtUtc = entry.OccurredAtUtc,
                EntryType = entry.EntryType,
                EntityId = entry.EntityId,
                VisitId = entry.VisitId,
                ReferenceNumber = entry.ReferenceNumber,
                Description = entry.Description,
                DeltaAmount = entry.DeltaAmount,
                RunningBalance = runningBalance,
                PaymentMethod = entry.PaymentMethod,
                PaymentStatus = entry.PaymentStatus,
                OrderStatus = entry.OrderStatus,
                ActorUserId = entry.ActorUserId,
                ActorUserName = entry.ActorUserName,
                Comment = entry.Comment
            });
        }

        var netChangeInRange = entriesInRange.Sum(x => x.DeltaAmount);
        var balanceAtRangeEnd = openingBalanceAtRangeStart + netChangeInRange;

        var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(customerId);
        if (balanceSnapshot is null)
        {
            return new CustomerQueryResult<CustomerFinancialLedgerResponseDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        return new CustomerQueryResult<CustomerFinancialLedgerResponseDto>
        {
            Status = CustomerQueryStatus.Success,
            Data = new CustomerFinancialLedgerResponseDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                CustomerCode = customer.Code,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                OpeningBalanceAtRangeStart = openingBalanceAtRangeStart,
                NetChangeInRange = netChangeInRange,
                BalanceAtRangeEnd = balanceAtRangeEnd,
                CurrentBalance = balanceSnapshot.CurrentBalance,
                TotalEntriesInRange = totalEntriesInRange,
                ReturnedEntries = returnedEntries.Count,
                IsTruncated = totalEntriesInRange > returnedEntries.Count,
                Entries = returnedEntries
            }
        };
    }

    public async Task<CustomerQueryResult<CustomerCreditProfileResponseDto>> GetCreditProfileAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles)
    {
        var customer = await _context.Customers
            .Include(x => x.AssignedSalesRep)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerQueryResult<CustomerCreditProfileResponseDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        if (!IsAdminOrManager(currentUserRoles) && customer.AssignedSalesRepId != currentUser.Id)
        {
            return new CustomerQueryResult<CustomerCreditProfileResponseDto>
            {
                Status = CustomerQueryStatus.Forbidden
            };
        }

        var balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(customerId);
        if (balanceSnapshot is null)
        {
            return new CustomerQueryResult<CustomerCreditProfileResponseDto>
            {
                Status = CustomerQueryStatus.CustomerNotFound
            };
        }

        var hasInProgressVisit = await _context.Visits
            .AsNoTracking()
            .AnyAsync(x =>
                x.CustomerId == customerId &&
                x.Status == VisitStatus.InProgress);

        var pendingPayments = await _context.Payments
            .AsNoTracking()
            .Where(x =>
                x.CustomerId == customerId &&
                x.Status == PaymentStatus.Pending)
            .Select(x => x.Amount)
            .ToListAsync();

        var pendingPaymentsCount = pendingPayments.Count;
        var pendingPaymentsAmount = pendingPayments.Sum();

        var approvalBlockedPendingPayments = pendingPayments
            .Where(amount => balanceSnapshot.CurrentBalance - amount < 0m)
            .ToList();

        var approvalBlockedPendingPaymentsCount = approvalBlockedPendingPayments.Count;
        var approvalBlockedPendingPaymentsAmount = approvalBlockedPendingPayments.Sum();

        var lastOrderDateUtc = await _context.Orders
            .Where(x =>
                x.CustomerId == customerId &&
                x.Status != OrderStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => (DateTime?)x.CreatedAtUtc)
            .FirstOrDefaultAsync();

        var lastApprovedPaymentDateUtc = await _context.Payments
            .Where(x =>
                x.CustomerId == customerId &&
                x.Status == PaymentStatus.Approved &&
                x.ReviewedAtUtc.HasValue)
            .OrderByDescending(x => x.ReviewedAtUtc)
            .Select(x => x.ReviewedAtUtc)
            .FirstOrDefaultAsync();

        decimal? remainingCredit = balanceSnapshot.CreditLimit > 0m
            ? balanceSnapshot.CreditLimit - balanceSnapshot.CurrentBalance
            : null;

        decimal? creditUtilizationRatio = balanceSnapshot.CreditLimit > 0m
            ? Math.Round(balanceSnapshot.CurrentBalance / balanceSnapshot.CreditLimit, 4)
            : null;

        decimal? overLimitAmount = null;

        if (balanceSnapshot.CreditLimit > 0m && balanceSnapshot.CurrentBalance > balanceSnapshot.CreditLimit)
        {
            overLimitAmount = balanceSnapshot.CurrentBalance - balanceSnapshot.CreditLimit;
        }
        else if (balanceSnapshot.CreditLimit <= 0m && balanceSnapshot.CurrentBalance > 0m)
        {
            overLimitAmount = balanceSnapshot.CurrentBalance;
        }

        var exposureLevel = BuildCustomerCreditExposureLevel(
            balanceSnapshot.CurrentBalance,
            balanceSnapshot.CreditLimit);

        var canOperate = customer.Status == CustomerStatus.Active;
        var hasConfiguredCreditLimit = balanceSnapshot.CreditLimit > 0m;
        var hasOutstandingBalance = balanceSnapshot.CurrentBalance > 0m;
        var canCreateOrder = canOperate &&
                             hasConfiguredCreditLimit &&
                             balanceSnapshot.CurrentBalance < balanceSnapshot.CreditLimit;
        var canCreatePayment = canOperate && hasOutstandingBalance;

        var requiresAdministrativeAttention =
            customer.Status != CustomerStatus.Active ||
            !hasConfiguredCreditLimit ||
            exposureLevel == CustomerCreditExposureLevel.NearCreditLimit ||
            exposureLevel == CustomerCreditExposureLevel.OverCreditLimit ||
            exposureLevel == CustomerCreditExposureLevel.UnboundedExposure ||
            approvalBlockedPendingPaymentsCount > 0;

        int? daysSinceLastApprovedPayment = null;

        if (lastApprovedPaymentDateUtc.HasValue)
        {
            daysSinceLastApprovedPayment =
                (int)Math.Floor((DateTime.UtcNow.Date - lastApprovedPaymentDateUtc.Value.Date).TotalDays);
        }

        var response = new CustomerCreditProfileResponseDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerCode = customer.Code,
            CustomerStatus = customer.Status,
            AssignedSalesRepId = customer.AssignedSalesRepId,
            AssignedSalesRepName = customer.AssignedSalesRep.FullName,
            OpeningBalance = balanceSnapshot.OpeningBalance,
            TotalOrders = balanceSnapshot.TotalOrders,
            ApprovedPayments = balanceSnapshot.ApprovedPayments,
            CurrentBalance = balanceSnapshot.CurrentBalance,
            CreditLimit = balanceSnapshot.CreditLimit,
            RemainingCredit = remainingCredit,
            CreditUtilizationRatio = creditUtilizationRatio,
            OverLimitAmount = overLimitAmount,
            ExposureLevel = exposureLevel,
            RequiresAdministrativeAttention = requiresAdministrativeAttention,
            CanStartVisit = canOperate,
            CanCreateOrder = canCreateOrder,
            CanCreatePayment = canCreatePayment,
            HasInProgressVisit = hasInProgressVisit,
            PendingPaymentsCount = pendingPaymentsCount,
            PendingPaymentsAmount = pendingPaymentsAmount,
            ApprovalBlockedPendingPaymentsCount = approvalBlockedPendingPaymentsCount,
            ApprovalBlockedPendingPaymentsAmount = approvalBlockedPendingPaymentsAmount,
            LastOrderDateUtc = lastOrderDateUtc,
            LastApprovedPaymentDateUtc = lastApprovedPaymentDateUtc,
            DaysSinceLastApprovedPayment = daysSinceLastApprovedPayment,
            RecommendedAction = BuildCustomerCreditRecommendedAction(
                customer.Status,
                exposureLevel,
                hasConfiguredCreditLimit,
                approvalBlockedPendingPaymentsCount)
        };

        return new CustomerQueryResult<CustomerCreditProfileResponseDto>
        {
            Status = CustomerQueryStatus.Success,
            Data = response
        };
    }

    private static bool IsAdminOrManager(IEnumerable<string> currentUserRoles)
    {
        return currentUserRoles.Contains(AppRoles.Admin) ||
               currentUserRoles.Contains(AppRoles.Manager);
    }

    private static CustomerCreditExposureLevel BuildCustomerCreditExposureLevel(
        decimal currentBalance,
        decimal creditLimit)
    {
        if (currentBalance <= 0m)
            return CustomerCreditExposureLevel.SettledOrCredit;

        if (creditLimit <= 0m)
            return CustomerCreditExposureLevel.UnboundedExposure;

        if (currentBalance > creditLimit)
            return CustomerCreditExposureLevel.OverCreditLimit;

        var utilizationRatio = currentBalance / creditLimit;

        if (utilizationRatio >= 0.90m)
            return CustomerCreditExposureLevel.NearCreditLimit;

        return CustomerCreditExposureLevel.OutstandingWithinLimit;
    }

    private static string BuildCustomerCreditRecommendedAction(
        CustomerStatus customerStatus,
        CustomerCreditExposureLevel exposureLevel,
        bool hasConfiguredCreditLimit,
        int approvalBlockedPendingPaymentsCount)
    {
        if (customerStatus != CustomerStatus.Active)
        {
            return "Customer is not active. Visits, orders, and payments are operationally blocked until the account is reactivated.";
        }

        if (!hasConfiguredCreditLimit)
        {
            return "Customer has no usable credit limit. New orders should remain blocked until credit policy is configured.";
        }

        if (exposureLevel == CustomerCreditExposureLevel.UnboundedExposure)
        {
            return "Customer has positive exposure without a valid credit limit. Review credit policy immediately before allowing further commercial exposure.";
        }

        if (exposureLevel == CustomerCreditExposureLevel.OverCreditLimit)
        {
            return "Customer is over credit limit. Review collections, pending payment approvals, and account status before allowing additional orders.";
        }

        if (exposureLevel == CustomerCreditExposureLevel.NearCreditLimit)
        {
            return "Customer is near credit limit. Monitor closely and review exposure before creating additional orders.";
        }

        if (approvalBlockedPendingPaymentsCount > 0)
        {
            return "Customer has pending payments that cannot be approved cleanly because they would create a negative balance. Review submissions before approval.";
        }

        if (exposureLevel == CustomerCreditExposureLevel.OutstandingWithinLimit)
        {
            return "Customer has outstanding exposure within credit policy. Continue operations with normal monitoring.";
        }

        return "Customer balance is settled or favorable. No immediate credit action is required.";
    }

    private static CustomerResponseDto MapCustomer(Mando.Api.Entities.Customer customer, string assignedSalesRepName)
    {
        return new CustomerResponseDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Code = customer.Code,
            ContactPersonName = customer.ContactPersonName,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            City = customer.City,
            Region = customer.Region,
            Latitude = customer.Latitude,
            Longitude = customer.Longitude,
            Status = customer.Status,
            CreditLimit = customer.CreditLimit,
            OpeningBalance = customer.OpeningBalance,
            Notes = customer.Notes,
            AssignedSalesRepId = customer.AssignedSalesRepId,
            AssignedSalesRepName = assignedSalesRepName,
            RowVersion = RowVersionTokenHelper.Encode(customer.RowVersion),
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc
        };
    }

    private sealed class CustomerFinancialLedgerRawEntry
    {
        public DateTime OccurredAtUtc { get; set; }
        public CustomerFinancialLedgerEntryType EntryType { get; set; }

        public Guid? EntityId { get; set; }
        public Guid? VisitId { get; set; }

        public string? ReferenceNumber { get; set; }
        public string Description { get; set; } = string.Empty;

        public decimal DeltaAmount { get; set; }

        public PaymentMethod? PaymentMethod { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public OrderStatus? OrderStatus { get; set; }

        public Guid? ActorUserId { get; set; }
        public string? ActorUserName { get; set; }

        public string? Comment { get; set; }
    }
}
