namespace Mando.Api.Enums;

public enum AuditActionType
{
    OrderCreated = 1,
    PaymentCreated = 2,
    PaymentApproved = 3,
    PaymentRejected = 4,
    CustomerStatusChanged = 5,
    UserStatusChanged = 6,
    ProductCreated = 7,
    ProductUpdated = 8,
    ProductStatusChanged = 9,
    CustomerCreated = 10,
    CustomerUpdated = 11,
    UserCreated = 12,
    OrderCancelled = 13,
    CustomerFinancialSettingsAdjusted = 14,
    VisitStarted = 15,
    VisitCompleted = 16,
    VisitCancelled = 17,
    UserRoleChanged = 18,
    VisitImageUploaded = 19,
    VisitImageDeleted = 20,
    PaymentReversed = 21
}
