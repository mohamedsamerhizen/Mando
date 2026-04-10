using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mando.Api.Common;
using Mando.Api.Data;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Users;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Users;
using Mando.Api.Models.Users;

namespace Mando.Api.Services.Users;

public class UserQueryService : IUserQueryService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;

    public UserQueryService(AppDbContext context, UserManager<AppUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UserQueryResult<PagedResultDto<UserResponseDto>>> GetAllAsync(GetUsersQueryDto query)
    {
        var normalizedPageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var normalizedPageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 200);

        IQueryable<AppUser> usersQuery = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            usersQuery = usersQuery.Where(x =>
                x.FullName.Contains(search) ||
                (x.Email != null && x.Email.Contains(search)));
        }

        if (query.IsActive.HasValue)
        {
            usersQuery = usersQuery.Where(x => x.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var normalizedRoleName = query.Role.Trim().ToUpperInvariant();

            usersQuery =
                from user in usersQuery
                join userRole in _context.UserRoles.AsNoTracking()
                    on user.Id equals userRole.UserId
                join role in _context.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where role.NormalizedName == normalizedRoleName
                select user;
        }

        var totalCount = await usersQuery.CountAsync();

        var pagedUsers = await usersQuery
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.Email)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new UserListItem
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email ?? string.Empty,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync();

        var userIds = pagedUsers
            .Select(x => x.Id)
            .ToList();

        var rolesByUserId = await GetRolesByUserIdsAsync(userIds);

        var items = pagedUsers
            .Select(user => new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                Roles = rolesByUserId.TryGetValue(user.Id, out var roles)
                    ? roles
                    : [],
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            })
            .ToList();

        return new UserQueryResult<PagedResultDto<UserResponseDto>>
        {
            Status = UserQueryStatus.Success,
            Data = new PagedResultDto<UserResponseDto>
            {
                Items = items,
                PageNumber = normalizedPageNumber,
                PageSize = normalizedPageSize,
                TotalCount = totalCount
            }
        };
    }

    public async Task<UserQueryResult<UserResponseDto>> GetByIdAsync(Guid userId)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new UserListItem
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email ?? string.Empty,
                IsActive = x.IsActive,
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return new UserQueryResult<UserResponseDto>
            {
                Status = UserQueryStatus.UserNotFound
            };
        }

        var rolesByUserId = await GetRolesByUserIdsAsync([user.Id]);

        return new UserQueryResult<UserResponseDto>
        {
            Status = UserQueryStatus.Success,
            Data = new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                Roles = rolesByUserId.TryGetValue(user.Id, out var roles)
                    ? roles
                    : [],
                CreatedAtUtc = user.CreatedAtUtc,
                UpdatedAtUtc = user.UpdatedAtUtc
            }
        };
    }

    public async Task<UserQueryResult<List<SalesRepLookupDto>>> GetSalesRepsAsync()
    {
        var salesRepRoleNormalizedName = AppRoles.SalesRep.ToUpperInvariant();

        var result = await (
            from user in _userManager.Users.AsNoTracking()
            join userRole in _context.UserRoles.AsNoTracking()
                on user.Id equals userRole.UserId
            join role in _context.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where user.IsActive && role.NormalizedName == salesRepRoleNormalizedName
            orderby user.FullName, user.Email
            select new SalesRepLookupDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            })
            .ToListAsync();

        return new UserQueryResult<List<SalesRepLookupDto>>
        {
            Status = UserQueryStatus.Success,
            Data = result
        };
    }

    public async Task<UserQueryResult<IReadOnlyList<UserActionHistoryResponseDto>>> GetHistoryAsync(Guid userId)
    {
        var userExists = await _userManager.Users
            .AsNoTracking()
            .AnyAsync(x => x.Id == userId);

        if (!userExists)
        {
            return new UserQueryResult<IReadOnlyList<UserActionHistoryResponseDto>>
            {
                Status = UserQueryStatus.UserNotFound
            };
        }

        var history = await _context.UserActionHistories
            .AsNoTracking()
            .Where(x => x.TargetUserId == userId)
            .OrderByDescending(x => x.ActionAtUtc)
            .Select(x => new UserActionHistoryResponseDto
            {
                Id = x.Id,
                TargetUserId = x.TargetUserId,
                ActionType = x.ActionType,
                FullNameSnapshot = x.FullNameSnapshot,
                EmailSnapshot = x.EmailSnapshot,
                RolesSnapshot = x.RolesSnapshot,
                PreviousIsActive = x.PreviousIsActive,
                NewIsActive = x.NewIsActive,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByUserName = x.PerformedByUserFullName,
                Comment = x.Comment,
                ActionAtUtc = x.ActionAtUtc
            })
            .ToListAsync();

        return new UserQueryResult<IReadOnlyList<UserActionHistoryResponseDto>>
        {
            Status = UserQueryStatus.Success,
            Data = history
        };
    }

    private async Task<Dictionary<Guid, List<string>>> GetRolesByUserIdsAsync(IReadOnlyCollection<Guid> userIds)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, List<string>>();

        var roleRows = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            orderby role.Name
            select new
            {
                userRole.UserId,
                RoleName = role.Name
            })
            .ToListAsync();

        return roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Where(x => !string.IsNullOrWhiteSpace(x.RoleName))
                    .Select(x => x.RoleName!)
                    .Distinct()
                    .ToList());
    }

    private sealed class UserListItem
    {
        public Guid Id { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime? UpdatedAtUtc { get; init; }
    }
}