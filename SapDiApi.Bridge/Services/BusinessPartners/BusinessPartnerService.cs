using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.Services.BusinessPartners
{
    public class BusinessPartnerService : IBusinessPartnerService
    {
        private readonly ISapDiApiConnector _sapConnector;
        private readonly ILogger<BusinessPartnerService> _logger;

        public BusinessPartnerService(ISapDiApiConnector sapConnector, ILogger<BusinessPartnerService> logger)
        {
            _sapConnector = sapConnector;
            _logger = logger;
        }

        public IQueryable<BusinessPartnerDto> GetBusinessPartnersQueryable()
        {
            return new List<BusinessPartnerDto>().AsQueryable();
        }

        public async Task<BusinessPartnerDto?> GetByCardCodeAsync(string cardCode, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de consulta a Socio de Negocio {CardCode} sin sesión activa.", cardCode);
                return null;
            }

            _logger.LogInformation("Consultando Socio de Negocio {CardCode} directamente en SAP DI API | Usuario: {User} | DB: {DB}",
                cardCode, session.UserName, session.CompanyDB);

            return await _sapConnector.GetBusinessPartnerAsync(session, cardCode);
        }

        public Task<IEnumerable<BusinessPartnerDto>> GetFilteredAsync(BusinessPartnerFilterDto filter, UserSession? session = null)
        {
            var emptyList = new List<BusinessPartnerDto>();
            return Task.FromResult<IEnumerable<BusinessPartnerDto>>(emptyList);
        }

        public async Task<(bool Success, string CardCode, string? ErrorMessage)> CreateAsync(BusinessPartnerDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, dto.CardCode, "Se requiere una sesión activa (B1SESSION) para crear socios de negocio en SAP.");
            }

            _logger.LogInformation("Creando nuevo Socio de Negocio {CardCode} ({CardName}) en SAP | Usuario: {User} | DB: {DB} | Operador: {AuditUser}",
                dto.CardCode, dto.CardName, session.UserName, session.CompanyDB, session.AuditUser ?? "N/A");

            return await _sapConnector.CreateBusinessPartnerAsync(session, dto);
        }

        public async Task<(bool Success, string CardCode, string? ErrorMessage)> UpdateAsync(string cardCode, BusinessPartnerDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, cardCode, "Se requiere una sesión activa (B1SESSION) para actualizar socios de negocio en SAP.");
            }

            _logger.LogInformation("Actualizando Socio de Negocio {CardCode} en SAP | Usuario: {User} | DB: {DB} | Operador: {AuditUser}",
                cardCode, session.UserName, session.CompanyDB, session.AuditUser ?? "N/A");

            return await _sapConnector.UpdateBusinessPartnerAsync(session, cardCode, dto);
        }
    }
}
