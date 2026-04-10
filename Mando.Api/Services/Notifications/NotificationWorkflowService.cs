using Microsoft.EntityFrameworkCore;
using Mando.Api.DTOs.Notifications;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Notifications;
using Mando.Api.Models.Notifications;

namespace Mando.Api.Services.Notifications;

public class NotificationWorkflowService : INotificationWorkflowService
{
    private readonly Data.AppDbContext _context;

    public NotificationWorkflowService(Data.AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationWorkflowResult<NotificationResponseDto>> MarkAsReadAsync(
        Guid notificationId,
        AppUser currentUser)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == currentUser.Id);

        if (notification is null)
        {
            return new NotificationWorkflowResult<NotificationResponseDto>
            {
                Status = NotificationWorkflowStatus.NotificationNotFound
            };
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;
            notification.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        return new NotificationWorkflowResult<NotificationResponseDto>
        {
            Status = NotificationWorkflowStatus.Success,
            Data = MapNotification(notification)
        };
    }

    public async Task<NotificationWorkflowResult<int>> MarkAllAsReadAsync(AppUser currentUser)
    {
        var unreadNotifications = await _context.Notifications
            .Where(x => x.UserId == currentUser.Id && !x.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0)
        {
            return new NotificationWorkflowResult<int>
            {
                Status = NotificationWorkflowStatus.Success,
                Data = 0
            };
        }

        var now = DateTime.UtcNow;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = now;
            notification.UpdatedAtUtc = now;
        }

        await _context.SaveChangesAsync();

        return new NotificationWorkflowResult<int>
        {
            Status = NotificationWorkflowStatus.Success,
            Data = unreadNotifications.Count
        };
    }

    private static NotificationResponseDto MapNotification(Notification notification)
    {
        return new NotificationResponseDto
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
        };
    }
}
