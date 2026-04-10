namespace Mando.Api.Enums;

public enum CustomerCreditExposureLevel
{
    SettledOrCredit = 1,
    OutstandingWithinLimit = 2,
    NearCreditLimit = 3,
    OverCreditLimit = 4,
    UnboundedExposure = 5
}