using Mando.Api.DTOs.Common;
using Mando.Api.DTOs.Users;
using Mando.Api.Models.Users;

namespace Mando.Api.Interfaces.Users;

public interface IUserQueryService
{
    Task<UserQueryResult<PagedResultDto<UserResponseDto>>> GetAllAsync(GetUsersQueryDto query);

    Task<UserQueryResult<UserResponseDto>> GetByIdAsync(Guid userId);

    Task<UserQueryResult<List<SalesRepLookupDto>>> GetSalesRepsAsync();

    Task<UserQueryResult<IReadOnlyList<UserActionHistoryResponseDto>>> GetHistoryAsync(Guid userId);
}