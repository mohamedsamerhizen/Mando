using Mando.Api.DTOs.Visits;
using Mando.Api.Entities.Identity;
using Mando.Api.Models.Visits;

namespace Mando.Api.Interfaces.Visits;

public interface IVisitWorkflowService
{
    Task<VisitWorkflowResult> StartAsync(StartVisitRequestDto request, AppUser currentUser);
    Task<VisitWorkflowResult> EndAsync(Guid visitId, EndVisitRequestDto request, AppUser currentUser);
    Task<VisitWorkflowResult> CancelAsync(Guid visitId, CancelVisitRequestDto request, AppUser currentUser);
}