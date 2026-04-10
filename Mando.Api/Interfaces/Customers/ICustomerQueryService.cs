using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Customers;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Customers;

namespace Mando.Api.Interfaces.Customers;

public interface ICustomerQueryService
{
    Task<CustomerQueryResult<PagedResultDto<CustomerResponseDto>>> GetAllAsync(
        GetCustomersQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<CustomerQueryResult<CustomerResponseDto>> GetByIdAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<CustomerQueryResult<CustomerBalanceDto>> GetBalanceAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<CustomerQueryResult<CustomerStatementResponseDto>> GetStatementAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<CustomerQueryResult<IReadOnlyList<CustomerActionHistoryResponseDto>>> GetHistoryAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<CustomerQueryResult<CustomerFinancialLedgerResponseDto>> GetFinancialLedgerAsync(
        Guid customerId,
        GetCustomerFinancialLedgerQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<CustomerQueryResult<CustomerCreditProfileResponseDto>> GetCreditProfileAsync(
        Guid customerId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);
}