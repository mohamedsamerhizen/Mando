using Mando.Api.DTOs.Orders;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Orders;

namespace Mando.Api.Interfaces.Orders;

public interface IOrderWorkflowService
{
    Task<OrderWorkflowResult> CreateAsync(CreateOrderRequestDto request, AppUser currentUser);

    Task<OrderWorkflowResult> CancelAsync(
        Guid orderId,
        CancelOrderRequestDto request,
        AppUser currentUser,
        IEnumerable<string> currentUserRoles);
}