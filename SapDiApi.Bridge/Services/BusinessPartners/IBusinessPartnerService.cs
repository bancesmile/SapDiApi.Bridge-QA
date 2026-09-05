using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;

namespace SapDiApi.Bridge.Services.BusinessPartners
{
    public interface IBusinessPartnerService
    {
        IQueryable<BusinessPartnerDto> GetBusinessPartnersQueryable();
        Task<BusinessPartnerDto?> GetByCardCodeAsync(string cardCode, UserSession? session = null);
        Task<IEnumerable<BusinessPartnerDto>> GetFilteredAsync(BusinessPartnerFilterDto filter, UserSession? session = null);
        Task<(bool Success, string CardCode, string? ErrorMessage)> CreateAsync(BusinessPartnerDto dto, UserSession? session = null);
        Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateAsync(string cardCode, BusinessPartnerDto dto, UserSession? session = null);
    }
}
