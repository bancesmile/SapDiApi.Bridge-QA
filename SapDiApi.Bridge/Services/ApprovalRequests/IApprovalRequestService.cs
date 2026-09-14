using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Auth;

namespace SapDiApi.Bridge.Services.ApprovalRequests
{
    public interface IApprovalRequestService
    {
        IQueryable<ApprovalRequestDto> GetApprovalRequestsQueryable();
        Task<ApprovalRequestDto?> GetByCodeAsync(int code, UserSession? session = null);
        Task<IEnumerable<ApprovalRequestDto>> GetFilteredAsync(ApprovalRequestFilterDto filter, UserSession? session = null);
        Task<(bool Success, int Code, string? ErrorMessage)> UpdateAsync(int code, UpdateApprovalRequestDto dto, UserSession? session = null);
    }
}
