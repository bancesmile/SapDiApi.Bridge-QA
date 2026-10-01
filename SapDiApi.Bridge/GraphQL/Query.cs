using HotChocolate;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Models.Health;
using SapDiApi.Bridge.Models.Users;
using SapDiApi.Bridge.Services.ApprovalRequests;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Drafts;
using SapDiApi.Bridge.Services.Health;
using SapDiApi.Bridge.Services.Users;

namespace SapDiApi.Bridge.GraphQL
{
    public class Query
    {
        /// <summary>
        /// Consulta GraphQL para obtener Socios de Negocio con proyección de campos, filtros y ordenamiento dinámico.
        /// </summary>
        [UseProjection]
        [UseFiltering]
        [UseSorting]
        public IQueryable<BusinessPartnerDto> GetBusinessPartners([Service] IBusinessPartnerService bpService)
        {
            return bpService.GetBusinessPartnersQueryable();
        }

        /// <summary>
        /// Consulta GraphQL por código único de Socio de Negocio conectado a SAP DI API mediante la sesión activa (B1SESSION).
        /// </summary>
        public async Task<BusinessPartnerDto?> GetBusinessPartnerByCardCode(
            string cardCode,
            [Service] IBusinessPartnerService bpService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await bpService.GetByCardCodeAsync(cardCode, session);
        }

        /// <summary>
        /// Consulta GraphQL para obtener Solicitudes de Aprobación con filtros dinámicos.
        /// </summary>
        public async Task<IEnumerable<ApprovalRequestDto>> GetApprovalRequests(
            ApprovalRequestFilterDto? filter,
            [Service] IApprovalRequestService approvalService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await approvalService.GetFilteredAsync(filter ?? new ApprovalRequestFilterDto(), session);
        }

        /// <summary>
        /// Consulta GraphQL por código de Solicitud de Aprobación en SAP Business One.
        /// </summary>
        public async Task<ApprovalRequestDto?> GetApprovalRequestByCode(
            int code,
            [Service] IApprovalRequestService approvalService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await approvalService.GetByCodeAsync(code, session);
        }

        /// <summary>
        /// Consulta GraphQL para listar documentos preliminares / borradores (Drafts) en SAP Business One.
        /// </summary>
        public async Task<IEnumerable<DraftDto>> GetDrafts(
            DraftFilterDto? filter,
            [Service] IDraftService draftService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await draftService.GetFilteredAsync(filter ?? new DraftFilterDto(), session);
        }

        /// <summary>
        /// Consulta GraphQL de un documento preliminar / borrador por DocEntry en SAP Business One.
        /// </summary>
        public async Task<DraftDto?> GetDraftByDocEntry(
            int docEntry,
            [Service] IDraftService draftService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await draftService.GetByDocEntryAsync(docEntry, session);
        }

        /// <summary>
        /// Consulta GraphQL para listar usuarios en SAP Business One con filtros administrativos.
        /// </summary>
        public async Task<IEnumerable<UserDto>> GetUsers(
            UserFilterDto? filter,
            [Service] IUserService userService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await userService.GetFilteredAsync(filter ?? new UserFilterDto(), session);
        }

        /// <summary>
        /// Consulta GraphQL de un usuario por InternalKey en SAP Business One.
        /// </summary>
        public async Task<UserDto?> GetUserById(
            int internalKey,
            bool includePermissions,
            [Service] IUserService userService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await userService.GetByIdAsync(internalKey, includePermissions, session);
        }

        /// <summary>
        /// Consulta GraphQL de un usuario por código de usuario (UserCode) en SAP Business One.
        /// </summary>
        public async Task<UserDto?> GetUserByCode(
            string userCode,
            bool includePermissions,
            [Service] IUserService userService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            return await userService.GetByCodeAsync(userCode, includePermissions, session);
        }

        /// <summary>
        /// Consulta el estado de salud, uptime y métricas del sistema BridgeSap.
        /// </summary>
        public HealthStatusDto GetHealth([Service] IHealthService healthService)
        {
            return healthService.GetHealthStatus();
        }

        /// <summary>
        /// Consulta el número de sesiones activas en memoria de SAP Service Layer / DI API.
        /// </summary>
        public int GetActiveSessionsCount([Service] ISessionManager sessionManager)
        {
            return sessionManager.ActiveSessionCount;
        }
    }
}

