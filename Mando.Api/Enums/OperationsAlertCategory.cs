namespace Mando.Api.Enums;

public enum OperationsAlertCategory
{
    PaymentApprovalBlocked = 1,
    PaymentStalePending = 2,
    PaymentMissingReference = 3,
    PaymentHighBalanceImpact = 4,
    PaymentDuplicateReference = 5,
    PaymentMultiplePending = 6,
    OrderStaleActive = 7,
    VisitStaleInProgress = 8,
    CustomerNearCreditLimit = 9,
    CustomerOverCreditLimit = 10
}