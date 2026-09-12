using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.Services.ApprovalRequests
{
    public class ApprovalRequestService : IApprovalRequestService
    {
        private readonly ISapDiApiConnector _sapConnector;
        private readonly ILogger<ApprovalRequestService> _logger;

        public ApprovalRequestService(ISapDiApiConnector sapConnector, ILogger<ApprovalRequestService> logger)
        {
            _sapConnector = sapConnector;
            _logger = logger;
        }

        public IQueryable<ApprovalRequestDto> GetApprovalRequestsQueryable()
        {
            return new List<ApprovalRequestDto>().AsQueryable();
        }

        public async Task<ApprovalRequestDto?> GetByCodeAsync(int code, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de consulta a Solicitud de Aprobación #{Code} sin sesión activa.", code);
                return null;
            }

            _logger.LogInformation("Consultando Solicitud de Aprobación #{Code} en SAP DI API | Usuario: {User} | DB: {DB}",
                code, session.UserName, session.CompanyDB);

            return await _sapConnector.GetApprovalRequestAsync(session, code);
        }

        public async Task<IEnumerable<ApprovalRequestDto>> GetFilteredAsync(ApprovalRequestFilterDto filter, UserSession? session = null)
        {
            if (session == null)
            {
                _logger.LogWarning("Intento de listar Solicitudes de Aprobación sin sesión activa.");
                return Enumerable.Empty<ApprovalRequestDto>();
            }

            return await _sapConnector.GetApprovalRequestsFilteredAsync(session, filter);
        }

        public async Task<(bool Success, int Code, string? ErrorMessage)> UpdateAsync(int code, UpdateApprovalRequestDto dto, UserSession? session = null)
        {
            if (session == null)
            {
                return (false, code, "Se requiere una sesión activa (B1SESSION) para autorizar o actualizar solicitudes en SAP.");
            }

            _logger.LogInformation("Actualizando Solicitud de Aprobación #{Code} en SAP | Usuario: {User} | DB: {DB} | Operador: {AuditUser}",
                code, session.UserName, session.CompanyDB, session.AuditUser ?? "N/A");

            return await _sapConnector.UpdateApprovalRequestAsync(session, code, dto);
        }
    }
}
