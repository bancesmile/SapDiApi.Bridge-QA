using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;

namespace SapDiApi.Bridge.Services.Sap
{
    public interface ISapDiApiConnector
    {
        (bool Connected, string ErrorMessage) Connect(UserSession session, string password);
        Task<BusinessPartnerDto?> GetBusinessPartnerAsync(UserSession session, string cardCode);
        Task<(bool Success, string CardCode, string? ErrorMessage)> CreateBusinessPartnerAsync(UserSession session, BusinessPartnerDto bpDto);
        Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateBusinessPartnerAsync(UserSession session, string cardCode, BusinessPartnerDto bpDto);
        Task<(bool Success, int AttachmentEntry, string? ErrorMessage)> CreateOrUpdateAttachmentAsync(UserSession session, AttachmentDto dto);
    }
}
