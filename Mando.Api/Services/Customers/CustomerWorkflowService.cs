
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Customers;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Customers;
using Mando.Api.Interfaces.Financials;
using Mando.Api.Models.Customers;
using Mando.Api.Models.Financials;
using Mando.Api.Interfaces.Users;

namespace Mando.Api.Services.Customers;

public class CustomerWorkflowService : ICustomerWorkflowService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWorkflowSideEffectService _workflowSideEffectService;
    private readonly ICustomerBalanceService _customerBalanceService;
    private readonly ICustomerFinancialLockService _customerFinancialLockService;
    private readonly IUserStatusLockService _userStatusLockService;

    public CustomerWorkflowService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        IWorkflowSideEffectService workflowSideEffectService,
        ICustomerBalanceService customerBalanceService,
        ICustomerFinancialLockService customerFinancialLockService,
        IUserStatusLockService userStatusLockService)
    {
        _context = context;
        _userManager = userManager;
        _workflowSideEffectService = workflowSideEffectService;
        _customerBalanceService = customerBalanceService;
        _customerFinancialLockService = customerFinancialLockService;
        _userStatusLockService = userStatusLockService;
    }

    public async Task<CustomerWorkflowResult> CreateAsync(CreateCustomerRequestDto request, AppUser currentUser)
    {
        var normalizedName = InputNormalizationHelper.NormalizeRequiredSingleLine(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerNameRequired
            };
        }

        var normalizedCode = InputNormalizationHelper.NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerCodeRequired
            };
        }

        if (!HasValidGeoCoordinates(request.Latitude, request.Longitude))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.InvalidGeoCoordinates
            };
        }

        var salesRepValidation = await ValidateSalesRepAsync(request.AssignedSalesRepId);
        if (salesRepValidation.Status != CustomerWorkflowStatus.Success)
            return salesRepValidation;

        var codeExists = await _context.Customers.AnyAsync(x => x.Code == normalizedCode);
        if (codeExists)
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerCodeAlreadyExists
            };
        }

        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Code = normalizedCode,
            ContactPersonName = InputNormalizationHelper.NormalizeOptionalSingleLine(request.ContactPersonName),
            PhoneNumber = InputNormalizationHelper.NormalizeOptionalSingleLine(request.PhoneNumber),
            Address = InputNormalizationHelper.NormalizeOptionalMultiline(request.Address),
            City = InputNormalizationHelper.NormalizeOptionalSingleLine(request.City),
            Region = InputNormalizationHelper.NormalizeOptionalSingleLine(request.Region),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Status = CustomerStatus.Active,
            CreditLimit = request.CreditLimit,
            OpeningBalance = request.OpeningBalance,
            Notes = InputNormalizationHelper.NormalizeOptionalMultiline(request.Notes),
            AssignedSalesRepId = request.AssignedSalesRepId,
            CreatedAtUtc = now
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var salesRepLockAcquired = await _userStatusLockService.LockAsync(request.AssignedSalesRepId);
            if (!salesRepLockAcquired)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.AssignedSalesRepNotFound
                };
            }

            salesRepValidation = await ValidateSalesRepAsync(request.AssignedSalesRepId);
            if (salesRepValidation.Status != CustomerWorkflowStatus.Success)
            {
                await transaction.RollbackAsync();
                return salesRepValidation;
            }

            _context.Customers.Add(customer);

            _context.CustomerActionHistories.Add(CreateHistoryEntry(
                customerId: customer.Id,
                actionType: CustomerActionType.Created,
                previousName: null,
                newName: customer.Name,
                previousCode: null,
                newCode: customer.Code,
                previousStatus: null,
                newStatus: customer.Status,
                previousAssignedSalesRepId: null,
                previousAssignedSalesRepName: null,
                newAssignedSalesRepId: customer.AssignedSalesRepId,
                newAssignedSalesRepName: salesRepValidation.AssignedSalesRepName!,
                previousCreditLimit: null,
                newCreditLimit: customer.CreditLimit,
                previousOpeningBalance: null,
                newOpeningBalance: customer.OpeningBalance,
                performedByUser: currentUser,
                comment: "Customer created."));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.CustomerCreated,
                nameof(Customer),
                customer.Id,
                $"Customer '{customer.Name}' with code '{customer.Code}' was created by '{currentUser.FullName}' and assigned to '{salesRepValidation.AssignedSalesRepName}'.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new CustomerWorkflowResult
        {
            Status = CustomerWorkflowStatus.Success,
            Customer = customer,
            AssignedSalesRepName = salesRepValidation.AssignedSalesRepName
        };
    }

    public async Task<CustomerWorkflowResult> UpdateAsync(Guid customerId, UpdateCustomerRequestDto request, AppUser currentUser)
    {
        var customer = await _context.Customers
            .Include(x => x.AssignedSalesRep)
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerNotFound
            };
        }

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.InvalidConcurrencyToken
            };
        }

        var normalizedName = InputNormalizationHelper.NormalizeRequiredSingleLine(request.Name);
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerNameRequired
            };
        }

        var normalizedCode = InputNormalizationHelper.NormalizeCode(request.Code);
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerCodeRequired
            };
        }

        if (!HasValidGeoCoordinates(request.Latitude, request.Longitude))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.InvalidGeoCoordinates
            };
        }

        var salesRepValidation = await ValidateSalesRepAsync(request.AssignedSalesRepId);
        if (salesRepValidation.Status != CustomerWorkflowStatus.Success)
            return salesRepValidation;

        var codeExists = await _context.Customers
            .AnyAsync(x => x.Code == normalizedCode && x.Id != customerId);

        if (codeExists)
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerCodeAlreadyExists
            };
        }

        _context.Entry(customer).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        var oldName = customer.Name;
        var oldCode = customer.Code;
        var oldStatus = customer.Status;
        var oldAssignedSalesRepId = customer.AssignedSalesRepId;
        var oldAssignedSalesRepName = customer.AssignedSalesRep.FullName;
        var oldCreditLimit = customer.CreditLimit;
        var oldOpeningBalance = customer.OpeningBalance;

        customer.Name = normalizedName;
        customer.Code = normalizedCode;
        customer.ContactPersonName = InputNormalizationHelper.NormalizeOptionalSingleLine(request.ContactPersonName);
        customer.PhoneNumber = InputNormalizationHelper.NormalizeOptionalSingleLine(request.PhoneNumber);
        customer.Address = InputNormalizationHelper.NormalizeOptionalMultiline(request.Address);
        customer.City = InputNormalizationHelper.NormalizeOptionalSingleLine(request.City);
        customer.Region = InputNormalizationHelper.NormalizeOptionalSingleLine(request.Region);
        customer.Latitude = request.Latitude;
        customer.Longitude = request.Longitude;
        customer.Notes = InputNormalizationHelper.NormalizeOptionalMultiline(request.Notes);
        customer.AssignedSalesRepId = request.AssignedSalesRepId;
        customer.UpdatedAtUtc = DateTime.UtcNow;

        var comment = BuildCustomerProfileUpdateComment(
            oldAssignedSalesRepId,
            customer.AssignedSalesRepId,
            oldAssignedSalesRepName,
            salesRepValidation.AssignedSalesRepName!);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var salesRepLockAcquired = await _userStatusLockService.LockAsync(request.AssignedSalesRepId);
            if (!salesRepLockAcquired)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.AssignedSalesRepNotFound
                };
            }

            salesRepValidation = await ValidateSalesRepAsync(request.AssignedSalesRepId);
            if (salesRepValidation.Status != CustomerWorkflowStatus.Success)
            {
                await transaction.RollbackAsync();
                return salesRepValidation;
            }

            _context.CustomerActionHistories.Add(CreateHistoryEntry(
                customerId: customer.Id,
                actionType: CustomerActionType.Updated,
                previousName: oldName,
                newName: customer.Name,
                previousCode: oldCode,
                newCode: customer.Code,
                previousStatus: oldStatus,
                newStatus: customer.Status,
                previousAssignedSalesRepId: oldAssignedSalesRepId,
                previousAssignedSalesRepName: oldAssignedSalesRepName,
                newAssignedSalesRepId: customer.AssignedSalesRepId,
                newAssignedSalesRepName: salesRepValidation.AssignedSalesRepName!,
                previousCreditLimit: oldCreditLimit,
                newCreditLimit: customer.CreditLimit,
                previousOpeningBalance: oldOpeningBalance,
                newOpeningBalance: customer.OpeningBalance,
                performedByUser: currentUser,
                comment: comment));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.CustomerUpdated,
                nameof(Customer),
                customer.Id,
                $"Customer '{customer.Name}' was updated by '{currentUser.FullName}'.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new CustomerWorkflowResult
        {
            Status = CustomerWorkflowStatus.Success,
            Customer = customer,
            AssignedSalesRepName = salesRepValidation.AssignedSalesRepName
        };
    }

    public async Task<CustomerWorkflowResult> AdjustFinancialSettingsAsync(
        Guid customerId,
        AdjustCustomerFinancialSettingsRequestDto request,
        AppUser currentUser)
    {
        var customer = await _context.Customers
            .Include(x => x.AssignedSalesRep)
            .FirstOrDefaultAsync(x => x.Id == customerId);

        if (customer is null)
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerNotFound
            };
        }

        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.InvalidConcurrencyToken
            };
        }

        var normalizedReason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerFinancialAdjustmentReasonRequired
            };
        }

        if (customer.CreditLimit == request.CreditLimit && customer.OpeningBalance == request.OpeningBalance)
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerFinancialSettingsUnchanged
            };
        }

        _context.Entry(customer).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

        var oldCreditLimit = customer.CreditLimit;
        var oldOpeningBalance = customer.OpeningBalance;
        CustomerBalanceSnapshot? balanceSnapshot = null;
        decimal projectedBalanceAfterAdjustment = 0m;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var customerLockAcquired = await _customerFinancialLockService.LockAsync(customerId);
            if (!customerLockAcquired)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.CustomerNotFound
                };
            }

            balanceSnapshot = await _customerBalanceService.GetSnapshotAsync(customerId);
            if (balanceSnapshot is null)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.CustomerNotFound
                };
            }

            projectedBalanceAfterAdjustment = request.OpeningBalance +
                                              balanceSnapshot.TotalOrders -
                                              balanceSnapshot.ApprovedPayments;

            if (projectedBalanceAfterAdjustment < 0m)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.CustomerOpeningBalanceAdjustmentWouldCreateNegativeBalance
                };
            }

            if (request.CreditLimit < projectedBalanceAfterAdjustment)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.CustomerCreditLimitWouldFallBelowProjectedOutstandingBalance
                };
            }

            customer.CreditLimit = request.CreditLimit;
            customer.OpeningBalance = request.OpeningBalance;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            var comment = BuildFinancialSettingsAdjustmentComment(
                oldCreditLimit,
                customer.CreditLimit,
                oldOpeningBalance,
                customer.OpeningBalance,
                balanceSnapshot.CurrentBalance,
                projectedBalanceAfterAdjustment,
                normalizedReason);

            _context.CustomerActionHistories.Add(CreateHistoryEntry(
                customerId: customer.Id,
                actionType: CustomerActionType.FinancialSettingsAdjusted,
                previousName: customer.Name,
                newName: customer.Name,
                previousCode: customer.Code,
                newCode: customer.Code,
                previousStatus: customer.Status,
                newStatus: customer.Status,
                previousAssignedSalesRepId: customer.AssignedSalesRepId,
                previousAssignedSalesRepName: customer.AssignedSalesRep.FullName,
                newAssignedSalesRepId: customer.AssignedSalesRepId,
                newAssignedSalesRepName: customer.AssignedSalesRep.FullName,
                previousCreditLimit: oldCreditLimit,
                newCreditLimit: customer.CreditLimit,
                previousOpeningBalance: oldOpeningBalance,
                newOpeningBalance: customer.OpeningBalance,
                performedByUser: currentUser,
                comment: comment));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.CustomerFinancialSettingsAdjusted,
                nameof(Customer),
                customer.Id,
                $"Customer '{customer.Name}' financial settings were adjusted by '{currentUser.FullName}'. Credit limit: {oldCreditLimit:0.00} -> {customer.CreditLimit:0.00}. Opening balance: {oldOpeningBalance:0.00} -> {customer.OpeningBalance:0.00}. Balance before adjustment: {balanceSnapshot.CurrentBalance:0.00}. Balance after adjustment: {projectedBalanceAfterAdjustment:0.00}. Reason: {normalizedReason}");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new CustomerWorkflowResult
        {
            Status = CustomerWorkflowStatus.Success,
            Customer = customer,
            AssignedSalesRepName = customer.AssignedSalesRep.FullName
        };
    }

    public async Task<CustomerWorkflowResult> ChangeStatusAsync(
        Guid customerId,
        ChangeCustomerStatusRequestDto request,
        AppUser currentUser)
    {
        if (!RowVersionTokenHelper.TryDecode(request.RowVersion, out var originalRowVersion))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.InvalidConcurrencyToken
            };
        }

        var trimmedReason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedReason))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.CustomerStatusReasonRequired
            };
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var customerLockAcquired = await _customerFinancialLockService.LockAsync(customerId);
            if (!customerLockAcquired)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.CustomerNotFound
                };
            }

            var customer = await _context.Customers
                .Include(x => x.AssignedSalesRep)
                .FirstOrDefaultAsync(x => x.Id == customerId);

            if (customer is null)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.CustomerNotFound
                };
            }

            if (customer.Status == request.Status)
            {
                await transaction.RollbackAsync();

                return new CustomerWorkflowResult
                {
                    Status = CustomerWorkflowStatus.CustomerStatusUnchanged,
                    Customer = customer,
                    AssignedSalesRepName = customer.AssignedSalesRep.FullName
                };
            }

            _context.Entry(customer).Property(x => x.RowVersion).OriginalValue = originalRowVersion;

            var oldStatus = customer.Status;
            var newStatus = request.Status;

            if (newStatus != CustomerStatus.Active)
            {
                var hasInProgressVisit = await _context.Visits
                    .AnyAsync(x =>
                        x.CustomerId == customerId &&
                        x.Status == VisitStatus.InProgress);

                if (hasInProgressVisit)
                {
                    await transaction.RollbackAsync();

                    return new CustomerWorkflowResult
                    {
                        Status = CustomerWorkflowStatus.CustomerHasInProgressVisit
                    };
                }

                var hasPendingPayments = await _context.Payments
                    .AnyAsync(x =>
                        x.CustomerId == customerId &&
                        x.Status == PaymentStatus.Pending);

                if (hasPendingPayments)
                {
                    await transaction.RollbackAsync();

                    return new CustomerWorkflowResult
                    {
                        Status = CustomerWorkflowStatus.CustomerHasPendingPayments
                    };
                }

                var hasSubmittedOrders = await _context.Orders
                    .AnyAsync(x =>
                        x.CustomerId == customerId &&
                        x.Status == OrderStatus.Submitted);

                if (hasSubmittedOrders)
                {
                    await transaction.RollbackAsync();

                    return new CustomerWorkflowResult
                    {
                        Status = CustomerWorkflowStatus.CustomerHasSubmittedOrders
                    };
                }
            }

            customer.Status = newStatus;
            customer.UpdatedAtUtc = DateTime.UtcNow;

            var statusChangeComment =
                $"Customer status changed from '{oldStatus}' to '{customer.Status}'. Reason: {trimmedReason}";

            _context.CustomerActionHistories.Add(CreateHistoryEntry(
                customerId: customer.Id,
                actionType: CustomerActionType.StatusChanged,
                previousName: customer.Name,
                newName: customer.Name,
                previousCode: customer.Code,
                newCode: customer.Code,
                previousStatus: oldStatus,
                newStatus: customer.Status,
                previousAssignedSalesRepId: customer.AssignedSalesRepId,
                previousAssignedSalesRepName: customer.AssignedSalesRep.FullName,
                newAssignedSalesRepId: customer.AssignedSalesRepId,
                newAssignedSalesRepName: customer.AssignedSalesRep.FullName,
                previousCreditLimit: customer.CreditLimit,
                newCreditLimit: customer.CreditLimit,
                previousOpeningBalance: customer.OpeningBalance,
                newOpeningBalance: customer.OpeningBalance,
                performedByUser: currentUser,
                comment: statusChangeComment));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.CustomerStatusChanged,
                nameof(Customer),
                customer.Id,
                $"Customer '{customer.Name}' status changed from '{oldStatus}' to '{customer.Status}' by '{currentUser.FullName}'. Reason: {trimmedReason}");

            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.Success,
                Customer = customer,
                AssignedSalesRepName = customer.AssignedSalesRep.FullName
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync();

            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.ConcurrencyConflict
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private CustomerActionHistory CreateHistoryEntry(
        Guid customerId,
        CustomerActionType actionType,
        string? previousName,
        string newName,
        string? previousCode,
        string newCode,
        CustomerStatus? previousStatus,
        CustomerStatus newStatus,
        Guid? previousAssignedSalesRepId,
        string? previousAssignedSalesRepName,
        Guid newAssignedSalesRepId,
        string newAssignedSalesRepName,
        decimal? previousCreditLimit,
        decimal newCreditLimit,
        decimal? previousOpeningBalance,
        decimal newOpeningBalance,
        AppUser performedByUser,
        string? comment)
    {
        return new CustomerActionHistory
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ActionType = actionType,
            PreviousName = previousName,
            NewName = newName,
            PreviousCode = previousCode,
            NewCode = newCode,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PreviousAssignedSalesRepId = previousAssignedSalesRepId,
            PreviousAssignedSalesRepName = previousAssignedSalesRepName,
            NewAssignedSalesRepId = newAssignedSalesRepId,
            NewAssignedSalesRepName = newAssignedSalesRepName,
            PreviousCreditLimit = previousCreditLimit,
            NewCreditLimit = newCreditLimit,
            PreviousOpeningBalance = previousOpeningBalance,
            NewOpeningBalance = newOpeningBalance,
            PerformedByUserId = performedByUser.Id,
            PerformedByUserFullName = performedByUser.FullName,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ActionAtUtc = DateTime.UtcNow
        };
    }

    private static string BuildCustomerProfileUpdateComment(
        Guid oldAssignedSalesRepId,
        Guid newAssignedSalesRepId,
        string oldAssignedSalesRepName,
        string newAssignedSalesRepName)
    {
        var changes = new List<string>();

        if (oldAssignedSalesRepId != newAssignedSalesRepId)
        {
            changes.Add($"assignment changed from '{oldAssignedSalesRepName}' to '{newAssignedSalesRepName}'");
        }

        return changes.Count == 0
            ? "Customer profile updated."
            : $"Customer updated: {string.Join("; ", changes)}.";
    }

    private static string BuildFinancialSettingsAdjustmentComment(
        decimal oldCreditLimit,
        decimal newCreditLimit,
        decimal oldOpeningBalance,
        decimal newOpeningBalance,
        decimal currentBalanceBeforeAdjustment,
        decimal currentBalanceAfterAdjustment,
        string reason)
    {
        return $"Customer financial settings adjusted. Credit limit: {oldCreditLimit:0.00} -> {newCreditLimit:0.00}; Opening balance: {oldOpeningBalance:0.00} -> {newOpeningBalance:0.00}; Current balance: {currentBalanceBeforeAdjustment:0.00} -> {currentBalanceAfterAdjustment:0.00}. Reason: {reason}";
    }

    private static bool HasValidGeoCoordinates(decimal latitude, decimal longitude)
    {
        return latitude >= -90m && latitude <= 90m &&
               longitude >= -180m && longitude <= 180m;
    }

    private async Task<CustomerWorkflowResult> ValidateSalesRepAsync(Guid assignedSalesRepId)
    {
        var salesRep = await _userManager.FindByIdAsync(assignedSalesRepId.ToString());
        if (salesRep is null)
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.AssignedSalesRepNotFound
            };
        }

        var salesRepRoles = await _userManager.GetRolesAsync(salesRep);
        if (!salesRepRoles.Contains(AppRoles.SalesRep))
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.AssignedUserNotSalesRep
            };
        }

        if (!salesRep.IsActive)
        {
            return new CustomerWorkflowResult
            {
                Status = CustomerWorkflowStatus.AssignedSalesRepInactive
            };
        }

        return new CustomerWorkflowResult
        {
            Status = CustomerWorkflowStatus.Success,
            AssignedSalesRepName = salesRep.FullName
        };
    }
}
