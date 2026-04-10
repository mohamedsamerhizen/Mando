using Microsoft.EntityFrameworkCore;
using Mando.Api.Data;
using Mando.Api.Entities;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Notifications;

namespace Mando.Api.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;

    public NotificationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task CreateForUserAsync(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            PaymentId = paymentId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    public async Task CreateForRolesAsync(
        string[] roles,
        NotificationType type,
        string title,
        string message,
        Guid? paymentId = null)
    {
        var distinctRoles = roles
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinctRoles.Length == 0)
            return;

        var targetUserIds = await
            (from user in _context.Users
             join userRole in _context.UserRoles on user.Id equals userRole.UserId
             join role in _context.Roles on userRole.RoleId equals role.Id
             where user.IsActive
                   && role.Name != null
                   && distinctRoles.Contains(role.Name)
             select user.Id)
            .Distinct()
            .ToListAsync();

        if (targetUserIds.Count == 0)
            return;

        var now = DateTime.UtcNow;

        var notifications = targetUserIds.Select(userId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            IsRead = false,
            PaymentId = paymentId,
            CreatedAtUtc = now
        });

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();
    }
}
