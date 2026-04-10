using Mando.Api.DTOs.Operations;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Operations;

namespace Mando.Api.Interfaces.Operations;

public interface IOperationsAlertWorkflowService
{
    Task<OperationsAlertReviewWorkflowResult> ReviewAsync(
        ReviewOperationsAlertRequestDto request,
        AppUser currentUser);
}