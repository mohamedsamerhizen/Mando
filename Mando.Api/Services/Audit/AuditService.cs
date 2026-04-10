
using Microsoft.AspNetCore.Identity;
using Mando.Api.Data;
using Mando.Api.Entities;
using Mando.Api.Entities.Identity;
using Mando.Api.Enums;
using Mando.Api.Interfaces.Audit;

namespace Mando.Api.Services.Audit;

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AppDbContext context,
        UserManager<AppUser> userManager,
        ILogger<AuditService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task CreateAsync(
        Guid? userId,
        AuditActionType actionType,
        string entityName,
        Guid entityId,
        string description)
    {
        AppUser? user = null;

        if (userId.HasValue)
        {
            user = await _userManager.FindByIdAsync(userId.Value.ToString());

            if (user is null)
            {
                _logger.LogWarning(
                    "Audit user lookup failed. UserId: {UserId} | ActionType: {ActionType} | Entity: {EntityName} | EntityId: {EntityId}",
                    userId.Value,
                    actionType,
                    entityName,
                    entityId);
            }
        }

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = user?.Id,
            UserFullName = user?.FullName,
            UserEmail = user?.Email,
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            Description = description,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        _logger.LogDebug(
            "Audit log persisted. ActionType: {ActionType} | Entity: {EntityName} | EntityId: {EntityId} | AuditLogId: {AuditLogId}",
            actionType,
            entityName,
            entityId,
            auditLog.Id);
    }
}
