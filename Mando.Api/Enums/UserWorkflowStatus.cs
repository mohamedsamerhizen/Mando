namespace Mando.Api.Enums;

public enum UserWorkflowStatus
{
    Success = 0,
    FullNameRequired = 1,
    EmailRequired = 2,
    PasswordRequired = 3,
    RoleRequired = 4,
    InvalidRole = 5,
    EmailAlreadyExists = 6,
    UserNotFound = 7,
    CannotDeactivateAdmin = 8,
    UserCreateFailed = 9,
    AssignRoleFailed = 10,
    UserUpdateFailed = 11,
    UserStatusReasonRequired = 12,
    CannotDeactivateUserWithAssignedActiveCustomers = 13,
    CannotDeactivateUserWithInProgressVisits = 14,
    UserStatusUnchanged = 15,
    CannotDeactivateUserWithPendingPayments = 16,
    CannotDeactivateUserWithSubmittedOrders = 17,
    UserRoleReasonRequired = 18,
    UserRoleUnchanged = 19,
    CannotChangeOwnRole = 20,
    CannotChangeAdminRole = 21,
    CannotChangeUserRoleWithAssignedActiveCustomers = 22,
    CannotChangeUserRoleWithInProgressVisits = 23,
    CannotChangeUserRoleWithPendingPayments = 24,
    CannotChangeUserRoleWithSubmittedOrders = 25
}
