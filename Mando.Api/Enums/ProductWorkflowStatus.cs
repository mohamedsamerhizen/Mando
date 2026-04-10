
namespace Mando.Api.Enums;

public enum ProductWorkflowStatus
{
    Success = 0,
    ProductNotFound = 1,
    ProductCodeAlreadyExists = 2,
    InvalidConcurrencyToken = 3,
    ConcurrencyConflict = 4,
    ProductNameRequired = 5,
    ProductCodeRequired = 6,
    ProductStatusUnchanged = 7
}

