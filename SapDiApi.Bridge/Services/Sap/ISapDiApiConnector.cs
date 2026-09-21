using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Models.Users;

namespace SapDiApi.Bridge.Services.Sap
{
    public interface ISapDiApiConnector
    {
        (bool Connected, string ErrorMessage) Connect(UserSession session, string password);
        Task<BusinessPartnerDto?> GetBusinessPartnerAsync(UserSession session, string cardCode);
        Task<(bool Success, string CardCode, string? ErrorMessage)> CreateBusinessPartnerAsync(UserSession session, BusinessPartnerDto bpDto);
        Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateBusinessPartnerAsync(UserSession session, string cardCode, BusinessPartnerDto bpDto);
        Task<(bool Success, int AttachmentEntry, string? ErrorMessage)> CreateOrUpdateAttachmentAsync(UserSession session, AttachmentDto dto);

        // ApprovalRequests (Autorizaciones)
        Task<ApprovalRequestDto?> GetApprovalRequestAsync(UserSession session, int code);
        Task<IEnumerable<ApprovalRequestDto>> GetApprovalRequestsFilteredAsync(UserSession session, ApprovalRequestFilterDto filter);
        Task<(bool Success, int Code, string? ErrorMessage)> UpdateApprovalRequestAsync(UserSession session, int code, UpdateApprovalRequestDto dto);

        // Drafts (Documentos Preliminares / Borradores)
        Task<DraftDto?> GetDraftAsync(UserSession session, int docEntry);
        Task<IEnumerable<DraftDto>> GetDraftsFilteredAsync(UserSession session, DraftFilterDto filter);
        Task<(bool Success, int DocEntry, int? GeneratedDocEntry, string? ErrorMessage)> SaveDraftToDocumentAsync(UserSession session, int docEntry);

        // Users (Administración de Usuarios OUSR)
        Task<UserDto?> GetUserByIdAsync(UserSession session, int internalKey, bool includePermissions = false);
        Task<UserDto?> GetUserByCodeAsync(UserSession session, string userCode, bool includePermissions = false);
        Task<IEnumerable<UserDto>> GetUsersFilteredAsync(UserSession session, Models.Users.UserFilterDto filter);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> CreateUserAsync(UserSession session, Models.Users.CreateUserDto dto);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateUserAsync(UserSession session, int internalKey, Models.Users.UpdateUserDto dto);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> UpdateUserByCodeAsync(UserSession session, string userCode, Models.Users.UpdateUserDto dto);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangeUserPasswordAsync(UserSession session, int internalKey, string newPassword);
        Task<(bool Success, int InternalKey, string? ErrorMessage)> ChangeUserPasswordByCodeAsync(UserSession session, string userCode, string newPassword);
    }
}

