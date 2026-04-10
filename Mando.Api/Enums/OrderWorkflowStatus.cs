namespace Mando.Api.Enums;

public enum OrderWorkflowStatus
{
    Success = 0,
    VisitNotFound = 1,
    Forbidden = 2,
    VisitNotInProgress = 3,
    CustomerInactive = 4,
    OrderItemsRequired = 5,
    InvalidOrInactiveProducts = 6,
    InvalidQuantity = 7,
    CreditLimitExceeded = 8,
    DuplicateProductsNotAllowed = 9,
    OrderNotFound = 10,
    OrderAlreadyCancelled = 11,
    CancellationReasonRequired = 12,
    InvalidConcurrencyToken = 13,
    ConcurrencyConflict = 14,
    OrderCancellationWouldCreateNegativeBalance = 15,
    SalesRepOrderCancellationWindowClosed = 16,
    CustomerNotFound = 17
}