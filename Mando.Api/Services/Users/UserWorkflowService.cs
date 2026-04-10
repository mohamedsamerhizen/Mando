
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Users;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Common;
using Mando.Api.Interfaces.Users;
using Mando.Api.Models.Users;

namespace Mando.Api.Services.Users;

public class UserWorkflowService : IUserWorkflowService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IWorkflowSideEffectService _workflowSideEffectService;
    private readonly IUserStatusLockService _userStatusLockService;

    public UserWorkflowService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        IWorkflowSideEffectService workflowSideEffectService,
        IUserStatusLockService userStatusLockService)
    {
        _context = context;
        _userManager = userManager;
        _workflowSideEffectService = workflowSideEffectService;
        _userStatusLockService = userStatusLockService;
    }

    public async Task<UserWorkflowResult> CreateAsync(CreateUserRequestDto request, AppUser currentUser)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password?.Trim() ?? string.Empty;
        var role = request.Role?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName))
            return new UserWorkflowResult { Status = UserWorkflowStatus.FullNameRequired };

        if (string.IsNullOrWhiteSpace(email))
            return new UserWorkflowResult { Status = UserWorkflowStatus.EmailRequired };

        if (string.IsNullOrWhiteSpace(password))
            return new UserWorkflowResult { Status = UserWorkflowStatus.PasswordRequired };

        if (string.IsNullOrWhiteSpace(role))
            return new UserWorkflowResult { Status = UserWorkflowStatus.RoleRequired };

        if (!AppRoles.All.Contains(role))
            return new UserWorkflowResult { Status = UserWorkflowStatus.InvalidRole };

        var emailExists = await _userManager.FindByEmailAsync(email);
        if (emailExists is not null)
            return new UserWorkflowResult { Status = UserWorkflowStatus.EmailAlreadyExists };

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            UserName = email,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserCreateFailed,
                    IdentityErrors = createResult.Errors.Select(x => x.Description).ToArray()
                };
            }

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.AssignRoleFailed,
                    User = user,
                    IdentityErrors = roleResult.Errors.Select(x => x.Description).ToArray()
                };
            }

            _context.UserActionHistories.Add(CreateHistoryEntry(
                targetUser: user,
                rolesSnapshot: role,
                actionType: UserActionType.Created,
                previousIsActive: null,
                newIsActive: user.IsActive,
                performedByUser: currentUser,
                comment: $"User created with role '{role}'."));

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            await _workflowSideEffectService.WriteAuditAsync(
                currentUser.Id,
                AuditActionType.UserCreated,
                nameof(AppUser),
                user.Id,
                $"User '{user.FullName}' with email '{user.Email}' was created by '{currentUser.FullName}' with role '{role}'.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return new UserWorkflowResult
        {
            Status = UserWorkflowStatus.Success,
            User = user,
            Roles = [role]
        };
    }

    public async Task<UserWorkflowResult> ChangeStatusAsync(
        Guid userId,
        ChangeUserStatusRequestDto request,
        AppUser? currentUser)
    {
        var trimmedReason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedReason))
        {
            return new UserWorkflowResult
            {
                Status = UserWorkflowStatus.UserStatusReasonRequired
            };
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userLockAcquired = await _userStatusLockService.LockAsync(userId);
            if (!userLockAcquired)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserNotFound
                };
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserNotFound
                };
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (user.IsActive == request.IsActive)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserStatusUnchanged,
                    User = user,
                    Roles = roles.ToList()
                };
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
            if (isAdmin && !request.IsActive)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.CannotDeactivateAdmin
                };
            }

            if (!request.IsActive)
            {
                var hasAssignedActiveCustomers = await _context.Customers
                    .AnyAsync(x =>
                        x.AssignedSalesRepId == user.Id &&
                        x.Status == CustomerStatus.Active);

                if (hasAssignedActiveCustomers)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotDeactivateUserWithAssignedActiveCustomers
                    };
                }

                var hasInProgressVisits = await _context.Visits
                    .AnyAsync(x =>
                        x.SalesRepId == user.Id &&
                        x.Status == VisitStatus.InProgress);

                if (hasInProgressVisits)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotDeactivateUserWithInProgressVisits
                    };
                }

                var hasPendingPayments = await _context.Payments
                    .AnyAsync(x =>
                        x.SalesRepId == user.Id &&
                        x.Status == PaymentStatus.Pending);

                if (hasPendingPayments)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotDeactivateUserWithPendingPayments
                    };
                }

                var hasSubmittedOrders = await _context.Orders
                    .AnyAsync(x =>
                        x.SalesRepId == user.Id &&
                        x.Status == OrderStatus.Submitted);

                if (hasSubmittedOrders)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotDeactivateUserWithSubmittedOrders
                    };
                }
            }

            var oldStatus = user.IsActive;

            user.IsActive = request.IsActive;
            user.UpdatedAtUtc = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserUpdateFailed,
                    User = user,
                    IdentityErrors = result.Errors.Select(x => x.Description).ToArray()
                };
            }

            var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!securityStampResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserUpdateFailed,
                    User = user,
                    IdentityErrors = securityStampResult.Errors.Select(x => x.Description).ToArray()
                };
            }

            if (currentUser is not null)
            {
                _context.UserActionHistories.Add(CreateHistoryEntry(
                    targetUser: user,
                    rolesSnapshot: string.Join(", ", roles.OrderBy(x => x)),
                    actionType: UserActionType.StatusChanged,
                    previousIsActive: oldStatus,
                    newIsActive: user.IsActive,
                    performedByUser: currentUser,
                    comment: $"User status changed from '{oldStatus}' to '{user.IsActive}'. Reason: {trimmedReason}"));

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            if (currentUser is not null)
            {
                await _workflowSideEffectService.WriteAuditAsync(
                    currentUser.Id,
                    AuditActionType.UserStatusChanged,
                    nameof(AppUser),
                    user.Id,
                    $"User '{user.FullName}' status changed from '{oldStatus}' to '{user.IsActive}' by '{currentUser.FullName}'. Reason: {trimmedReason}");
            }

            return new UserWorkflowResult
            {
                Status = UserWorkflowStatus.Success,
                User = user,
                Roles = roles.ToList()
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<UserWorkflowResult> ChangeRoleAsync(
        Guid userId,
        ChangeUserRoleRequestDto request,
        AppUser? currentUser)
    {
        var normalizedReason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedReason))
        {
            return new UserWorkflowResult
            {
                Status = UserWorkflowStatus.UserRoleReasonRequired
            };
        }

        var requestedRole = request.Role?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(requestedRole))
        {
            return new UserWorkflowResult
            {
                Status = UserWorkflowStatus.RoleRequired
            };
        }

        var normalizedRole = AppRoles.All.FirstOrDefault(x =>
            string.Equals(x, requestedRole, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(normalizedRole))
        {
            return new UserWorkflowResult
            {
                Status = UserWorkflowStatus.InvalidRole
            };
        }

        if (currentUser is not null && currentUser.Id == userId)
        {
            return new UserWorkflowResult
            {
                Status = UserWorkflowStatus.CannotChangeOwnRole
            };
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userLockAcquired = await _userStatusLockService.LockAsync(userId);
            if (!userLockAcquired)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserNotFound
                };
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user is null)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserNotFound
                };
            }

            var currentRoles = (await _userManager.GetRolesAsync(user))
                .OrderBy(x => x)
                .ToList();

            if (currentRoles.Contains(normalizedRole))
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserRoleUnchanged,
                    User = user,
                    Roles = currentRoles
                };
            }

            if (currentRoles.Contains(AppRoles.Admin) && !string.Equals(normalizedRole, AppRoles.Admin, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.CannotChangeAdminRole,
                    User = user,
                    Roles = currentRoles
                };
            }

            var currentUserIsSalesRep = currentRoles.Contains(AppRoles.SalesRep);
            var targetRoleIsSalesRep = string.Equals(normalizedRole, AppRoles.SalesRep, StringComparison.Ordinal);

            if (currentUserIsSalesRep && !targetRoleIsSalesRep)
            {
                var hasAssignedActiveCustomers = await _context.Customers
                    .AnyAsync(x =>
                        x.AssignedSalesRepId == user.Id &&
                        x.Status == CustomerStatus.Active);

                if (hasAssignedActiveCustomers)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotChangeUserRoleWithAssignedActiveCustomers,
                        User = user,
                        Roles = currentRoles
                    };
                }

                var hasInProgressVisits = await _context.Visits
                    .AnyAsync(x =>
                        x.SalesRepId == user.Id &&
                        x.Status == VisitStatus.InProgress);

                if (hasInProgressVisits)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotChangeUserRoleWithInProgressVisits,
                        User = user,
                        Roles = currentRoles
                    };
                }

                var hasPendingPayments = await _context.Payments
                    .AnyAsync(x =>
                        x.SalesRepId == user.Id &&
                        x.Status == PaymentStatus.Pending);

                if (hasPendingPayments)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotChangeUserRoleWithPendingPayments,
                        User = user,
                        Roles = currentRoles
                    };
                }

                var hasSubmittedOrders = await _context.Orders
                    .AnyAsync(x =>
                        x.SalesRepId == user.Id &&
                        x.Status == OrderStatus.Submitted);

                if (hasSubmittedOrders)
                {
                    await transaction.RollbackAsync();

                    return new UserWorkflowResult
                    {
                        Status = UserWorkflowStatus.CannotChangeUserRoleWithSubmittedOrders,
                        User = user,
                        Roles = currentRoles
                    };
                }
            }

            var removeResult = currentRoles.Count == 0
                ? IdentityResult.Success
                : await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.AssignRoleFailed,
                    User = user,
                    Roles = currentRoles,
                    IdentityErrors = removeResult.Errors.Select(x => x.Description).ToArray()
                };
            }

            var addResult = await _userManager.AddToRoleAsync(user, normalizedRole);
            if (!addResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.AssignRoleFailed,
                    User = user,
                    Roles = currentRoles,
                    IdentityErrors = addResult.Errors.Select(x => x.Description).ToArray()
                };
            }

            user.UpdatedAtUtc = DateTime.UtcNow;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserUpdateFailed,
                    User = user,
                    Roles = [normalizedRole],
                    IdentityErrors = updateResult.Errors.Select(x => x.Description).ToArray()
                };
            }

            var securityStampResult = await _userManager.UpdateSecurityStampAsync(user);
            if (!securityStampResult.Succeeded)
            {
                await transaction.RollbackAsync();

                return new UserWorkflowResult
                {
                    Status = UserWorkflowStatus.UserUpdateFailed,
                    User = user,
                    Roles = [normalizedRole],
                    IdentityErrors = securityStampResult.Errors.Select(x => x.Description).ToArray()
                };
            }

            if (currentUser is not null)
            {
                _context.UserActionHistories.Add(CreateHistoryEntry(
                    targetUser: user,
                    rolesSnapshot: normalizedRole,
                    actionType: UserActionType.RoleChanged,
                    previousIsActive: user.IsActive,
                    newIsActive: user.IsActive,
                    performedByUser: currentUser,
                    comment: $"User role changed from '{string.Join(", ", currentRoles)}' to '{normalizedRole}'. Reason: {normalizedReason}"));

                await _context.SaveChangesAsync();
            }

            await transaction.CommitAsync();

            if (currentUser is not null)
            {
                await _workflowSideEffectService.WriteAuditAsync(
                    currentUser.Id,
                    AuditActionType.UserRoleChanged,
                    nameof(AppUser),
                    user.Id,
                    $"User '{user.FullName}' role changed from '{string.Join(", ", currentRoles)}' to '{normalizedRole}' by '{currentUser.FullName}'. Reason: {normalizedReason}");
            }

            return new UserWorkflowResult
            {
                Status = UserWorkflowStatus.Success,
                User = user,
                Roles = [normalizedRole]
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private UserActionHistory CreateHistoryEntry(
        AppUser targetUser,
        string rolesSnapshot,
        UserActionType actionType,
        bool? previousIsActive,
        bool newIsActive,
        AppUser performedByUser,
        string? comment)
    {
        return new UserActionHistory
        {
            Id = Guid.NewGuid(),
            TargetUserId = targetUser.Id,
            ActionType = actionType,
            FullNameSnapshot = targetUser.FullName,
            EmailSnapshot = targetUser.Email ?? string.Empty,
            RolesSnapshot = rolesSnapshot,
            PreviousIsActive = previousIsActive,
            NewIsActive = newIsActive,
            PerformedByUserId = performedByUser.Id,
            PerformedByUserFullName = performedByUser.FullName,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            ActionAtUtc = DateTime.UtcNow
        };
    }
}
