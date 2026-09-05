using HotChocolate;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.GraphQL
{
    public class Mutation
    {
        /// <summary>
        /// Inicia sesión en SAP Business One generando un token de sesión B1SESSION.
        /// </summary>
        public async Task<LoginResponseDto> Login(
            LoginRequestDto request,
            [Service] ISapAuthService sapAuthService,
            [Service] ISessionManager sessionManager,
            [Service] IHttpContextAccessor httpContextAccessor)
        {
            var (success, errorMessage) = await sapAuthService.ValidateCredentialsAsync(request);
            if (!success)
            {
                throw new GraphQLException(errorMessage ?? "Credenciales de SAP incorrectas.");
            }

            var httpContext = httpContextAccessor.HttpContext;
            var clientIp = httpContext?.Connection.RemoteIpAddress?.ToString();
            var timeoutMinutes = 30;
            var executionMode = !string.IsNullOrWhiteSpace(request.AuditUser) ? "ServicePool" : "Direct";

            var session = sessionManager.CreateSession(
                companyDb: request.CompanyDB,
                userName: request.UserName,
                password: request.Password,
                clientIp: clientIp,
                timeoutMinutes: timeoutMinutes,
                auditUser: request.AuditUser,
                auditApp: request.AuditApp,
                executionMode: executionMode);

            if (httpContext != null)
            {
                httpContext.Response.Cookies.Append("B1SESSION", session.SessionId, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(timeoutMinutes),
                    Path = "/"
                });
            }

            return new LoginResponseDto
            {
                SessionId = session.SessionId,
                Version = "10.0",
                SessionTimeout = timeoutMinutes
            };
        }

        /// <summary>
        /// Cierra la sesión activa en SAP liberando recursos de memoria y cookies.
        /// </summary>
        public bool Logout(
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            sessionManager.TerminateSession(session.SessionId);

            var httpContext = httpContextAccessor.HttpContext;
            httpContext?.Response.Cookies.Delete("B1SESSION", new CookieOptions { Path = "/" });

            return true;
        }

        /// <summary>
        /// Crea un nuevo Socio de Negocio en SAP Business One (OCRD).
        /// </summary>
        public async Task<BusinessPartnerMutationResult> CreateBusinessPartner(
            BusinessPartnerDto input,
            [Service] IBusinessPartnerService bpService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, cardCode, errorMessage) = await bpService.CreateAsync(input, session);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? "Error desconocido al crear socio de negocio en SAP.");
            }

            return new BusinessPartnerMutationResult
            {
                Success = success,
                CardCode = cardCode,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Actualiza un Socio de Negocio existente en SAP Business One.
        /// </summary>
        public async Task<BusinessPartnerMutationResult> UpdateBusinessPartner(
            string cardCode,
            BusinessPartnerDto input,
            [Service] IBusinessPartnerService bpService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, resultCardCode, errorMessage) = await bpService.UpdateAsync(cardCode, input, session);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? "Error desconocido al actualizar socio de negocio en SAP.");
            }

            return new BusinessPartnerMutationResult
            {
                Success = success,
                CardCode = resultCardCode,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Crea o actualiza un lote de anexos físicos (Attachments2 / OATC) en SAP Business One.
        /// </summary>
        public async Task<AttachmentMutationResult> CreateAttachment(
            AttachmentDto input,
            [Service] ISapDiApiConnector sapConnector,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, entry, errorMessage) = await sapConnector.CreateOrUpdateAttachmentAsync(session, input);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? "Error al procesar anexo en SAP.");
            }

            return new AttachmentMutationResult
            {
                Success = success,
                AbsoluteEntry = entry,
                ErrorMessage = errorMessage
            };
        }
    }

    public class BusinessPartnerMutationResult
    {
        public bool Success { get; set; }
        public string CardCode { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class AttachmentMutationResult
    {
        public bool Success { get; set; }
        public int AbsoluteEntry { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
