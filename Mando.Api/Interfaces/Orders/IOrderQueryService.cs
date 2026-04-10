using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Orders;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Orders;

namespace Mando.Api.Interfaces.Orders;

public interface IOrderQueryService
{
    Task<OrderQueryResult<PagedResultDto<OrderResponseDto>>> GetAllAsync(
        GetOrdersQueryDto query,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<OrderQueryResult<OrderResponseDto>> GetByIdAsync(
        Guid orderId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<OrderQueryResult<IReadOnlyList<OrderActionHistoryResponseDto>>> GetHistoryAsync(
        Guid orderId,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);

    Task<OrderQueryResult<OrderOperationsReportResponseDto>> GetOperationsReportAsync(
        GetOrderOperationsReportQueryDto query);
}