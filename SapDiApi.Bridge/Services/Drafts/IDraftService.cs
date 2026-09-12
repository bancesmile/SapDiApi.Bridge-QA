using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Drafts;

namespace SapDiApi.Bridge.Services.Drafts
{
    public interface IDraftService
    {
        IQueryable<DraftDto> GetDraftsQueryable();
        Task<DraftDto?> GetByDocEntryAsync(int docEntry, UserSession? session = null);
        Task<IEnumerable<DraftDto>> GetFilteredAsync(DraftFilterDto filter, UserSession? session = null);
        Task<(bool Success, int DocEntry, int? GeneratedDocEntry, string? ErrorMessage)> SaveDraftToDocumentAsync(int docEntry, UserSession? session = null);
    }
}
