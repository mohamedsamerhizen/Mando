namespace Mando.Api.Enums;

public enum PaymentReviewRiskFlag
{
    Stale = 1,
    HighBalanceImpact = 2,
    MissingReferenceForNonCash = 3,
    MultiplePendingPaymentsForCustomer = 4,
    DuplicateReferenceInPendingQueue = 5,
    ApprovalBlockedByBalance = 6
}