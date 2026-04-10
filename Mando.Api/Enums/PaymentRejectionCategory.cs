namespace Mando.Api.Enums;

public enum PaymentRejectionCategory
{
    DocumentationIssue = 1,
    DuplicateSubmission = 2,
    AmountMismatch = 3,
    ReferenceMismatch = 4,
    UnsupportedPaymentEvidence = 5,
    OutsideCustomerBalance = 6,
    SuspiciousOrNeedsInvestigation = 7,
    Other = 8
}