using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.Services.Drafts
{
    public class DraftService : IDraftService
    {
        private readonly ISapDiApiConnector _sapConnector;
        private readonly ILogger<DraftService> _logger;

        public DraftService(ISapDiApiConnector sapConnector, ILogger<DraftService> logger)
        {
            _sapConnector = sapConnector;
            _logger = logger;
        }

        public IQueryable<DraftDto> GetDraftsQueryable()
        {
            return new List<DraftDto>().AsQueryable();
        }

        public async Task<DraftDto?> GetByDocEntryAsync(int docEntry, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de consulta a Borrador #{DocEntry} sin sesión activa.", docEntry);
                return null;
            }

            _logger.LogInformation("Consultando Borrador #{DocEntry} en SAP DI API | Usuario: {User} | DB: {DB}",
                docEntry, session.UserName, session.CompanyDB);

            return await _sapConnector.GetDraftAsync(session, docEntry);
        }

        public async Task<IEnumerable<DraftDto>> GetFilteredAsync(DraftFilterDto filter, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de listar Borradores sin sesión activa.");
                return Enumerable.Empty<DraftDto>();
            }

            return await _sapConnector.GetDraftsFilteredAsync(session, filter);
        }

        public async Task<(bool Success, int DocEntry, int? GeneratedDocEntry, string? ErrorMessage)> SaveDraftToDocumentAsync(int docEntry, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, docEntry, null, "Se requiere una sesión activa (B1SESSION) para convertir borradores a documentos finales en SAP.");
            }

            _logger.LogInformation("Convirtiendo Borrador #{DocEntry} en documento real en SAP | Usuario: {User} | DB: {DB} | Operador: {AuditUser}",
                docEntry, session.UserName, session.CompanyDB, session.AuditUser ?? "N/A");

            return await _sapConnector.SaveDraftToDocumentAsync(session, docEntry);
        }
    }
}
