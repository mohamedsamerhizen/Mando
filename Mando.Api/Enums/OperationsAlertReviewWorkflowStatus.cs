namespace Mando.Api.Enums;

public enum OperationsAlertReviewWorkflowStatus
{
    Success = 0,
    AlertFingerprintRequired = 1,
    InvalidAlertFingerprint = 2,
    AlertNotFound = 3,
    InvalidReviewStatus = 4,
    ReviewCommentRequired = 5,
    AlertAlreadyClosed = 6,
    AlertAlreadyInRequestedState = 7
}