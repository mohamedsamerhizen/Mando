namespace Mando.Api.Enums;

public enum VisitWorkflowStatus
{
    Success = 0,
    CustomerNotFound = 1,
    Forbidden = 2,
    CustomerInactive = 3,
    WeakLocationAccuracy = 4,
    OutOfRange = 5,
    ActiveVisitExists = 6,
    VisitNotFound = 7,
    VisitNotInProgress = 8,
    VisitHasOrders = 9,
    VisitHasPayments = 10,
    InvalidOutcome = 11,
    InvalidConcurrencyToken = 12,
    ConcurrencyConflict = 13
}