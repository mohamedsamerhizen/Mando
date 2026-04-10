using Mando.Api.Enums;

namespace Mando.Api.DTOs.Customers;

public class CustomerActionHistoryResponseDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }

    public CustomerActionType ActionType { get; set; }

    public string? PreviousName { get; set; }
    public string NewName { get; set; } = string.Empty;

    public string? PreviousCode { get; set; }
    public string NewCode { get; set; } = string.Empty;

    public CustomerStatus? PreviousStatus { get; set; }
    public CustomerStatus NewStatus { get; set; }

    public Guid? PreviousAssignedSalesRepId { get; set; }
    public string? PreviousAssignedSalesRepName { get; set; }

    public Guid NewAssignedSalesRepId { get; set; }
    public string NewAssignedSalesRepName { get; set; } = string.Empty;

    public decimal? PreviousCreditLimit { get; set; }
    public decimal NewCreditLimit { get; set; }

    public decimal? PreviousOpeningBalance { get; set; }
    public decimal NewOpeningBalance { get; set; }

    public Guid PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = string.Empty;

    public string? Comment { get; set; }
    public DateTime ActionAtUtc { get; set; }
}