using Mando.Api.DTOs.Customers;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Customers;

namespace Mando.Api.Interfaces.Customers;

public interface ICustomerWorkflowService
{
    Task<CustomerWorkflowResult> CreateAsync(CreateCustomerRequestDto request, AppUser currentUser);

    Task<CustomerWorkflowResult> UpdateAsync(Guid customerId, UpdateCustomerRequestDto request, AppUser currentUser);

    Task<CustomerWorkflowResult> AdjustFinancialSettingsAsync(
        Guid customerId,
        AdjustCustomerFinancialSettingsRequestDto request,
        AppUser currentUser);

    Task<CustomerWorkflowResult> ChangeStatusAsync(
        Guid customerId,
        ChangeCustomerStatusRequestDto request,
        AppUser currentUser);
}