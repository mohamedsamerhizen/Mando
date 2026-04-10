using Microsoft.EntityFrameworkCore;
using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Notifications;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Helpers;
using Mando.Api.Interfaces.Notifications;
using Mando.Api.Models.Notifications;

namespace Mando.Api.Services.Notifications;

public class NotificationQueryService : INotificationQueryService
{
    private readonly Data.AppDbContext _context;

    public NotificationQueryService(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationQueryResult<PagedResultDto<NotificationResponseDto>>> GetMyNotificationsAsync(
        GetNotificationsQueryDto query,
        AppUser currentUser)
    {
        var notificationsQuery = _context.Notifications
            .Where(x => x.UserId == currentUser.Id);

        if (query.IsRead.HasValue)
        {
            notificationsQuery = notificationsQuery.Where(x => x.IsRead == query.IsRead.Value);
        }

        var projectedQuery = notificationsQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotificationResponseDto
            {
                Id = x.Id,
                Type = x.Type,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAtUtc = x.CreatedAtUtc,
                ReadAtUtc = x.ReadAtUtc,
                ReferenceType = x.PaymentId.HasValue ? NotificationReferenceType.Payment : NotificationReferenceType.None,
                ReferenceId = x.PaymentId,
                PaymentId = x.PaymentId
            })
            .AsNoTracking();

        var result = await projectedQuery.ToPagedResultAsync(query.PageNumber, query.PageSize);

        return new NotificationQueryResult<PagedResultDto<NotificationResponseDto>>
        {
            Status = NotificationQueryStatus.Success,
            Data = result
        };
    }

    public async Task<NotificationQueryResult<NotificationResponseDto>> GetByIdAsync(
        Guid notificationId,
        AppUser currentUser)
    {
        var notification = await _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == currentUser.Id);

        if (notification is null)
        {
            return new NotificationQueryResult<NotificationResponseDto>
            {
                Status = NotificationQueryStatus.NotificationNotFound
            };
        }

        return new NotificationQueryResult<NotificationResponseDto>
        {
            Status = NotificationQueryStatus.Success,
            Data = new NotificationResponseDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAtUtc = notification.CreatedAtUtc,
                ReadAtUtc = notification.ReadAtUtc,
                ReferenceType = notification.PaymentId.HasValue ? NotificationReferenceType.Payment : NotificationReferenceType.None,
                ReferenceId = notification.PaymentId,
                PaymentId = notification.PaymentId
            }
        };
    }

    public async Task<NotificationQueryResult<NotificationUnreadSummaryResponseDto>> GetMyUnreadSummaryAsync(AppUser currentUser)
    {
        var baseQuery = _context.Notifications
            .Where(x => x.UserId == currentUser.Id);

        var unreadQuery = baseQuery
            .Where(x => !x.IsRead);

        var groupedUnread = await unreadQuery
            .GroupBy(x => x.Type)
            .Select(group => new
            {
                Type = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        var summary = new NotificationUnreadSummaryResponseDto
        {
            TotalCount = await baseQuery.CountAsync(),
            UnreadCount = groupedUnread.Sum(x => x.Count),
            LatestUnreadCreatedAtUtc = await unreadQuery
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => (DateTime?)x.CreatedAtUtc)
                .FirstOrDefaultAsync(),
            UnreadByType = groupedUnread.ToDictionary(x => x.Type, x => x.Count)
        };

        return new NotificationQueryResult<NotificationUnreadSummaryResponseDto>
        {
            Status = NotificationQueryStatus.Success,
            Data = summary
        };
    }
}
