using HotChocolate;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Health;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Health;

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
