namespace Mando.Api.Enums;

public enum CustomerWorkflowStatus
{
    Success = 0,
    CustomerNotFound = 1,
    AssignedSalesRepNotFound = 2,
    AssignedUserNotSalesRep = 3,
    CustomerCodeAlreadyExists = 4,
    InvalidConcurrencyToken = 5,
    ConcurrencyConflict = 6,
    CustomerStatusReasonRequired = 7,
    CustomerHasInProgressVisit = 8,
    CustomerHasPendingPayments = 9,
    CustomerFinancialAdjustmentReasonRequired = 10,
    CustomerFinancialSettingsUnchanged = 11,
    CustomerOpeningBalanceAdjustmentWouldCreateNegativeBalance = 12,
    CustomerCreditLimitWouldFallBelowProjectedOutstandingBalance = 13,
    CustomerNameRequired = 14,
    CustomerCodeRequired = 15,
    CustomerStatusUnchanged = 16,
    CustomerHasSubmittedOrders = 17,
    AssignedSalesRepInactive = 18,
    InvalidGeoCoordinates = 19
}
