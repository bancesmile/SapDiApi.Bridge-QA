using HotChocolate;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Users;
using SapDiApi.Bridge.Services.ApprovalRequests;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.BusinessPartners;
using SapDiApi.Bridge.Services.Drafts;
using SapDiApi.Bridge.Services.Sap;
using SapDiApi.Bridge.Services.Users;

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
        /// Actualiza o asienta decisiones (aprobar/rechazar) en una Solicitud de Aprobación en SAP Business One.
        /// </summary>
        public async Task<ApprovalRequestMutationResult> UpdateApprovalRequest(
            int code,
            UpdateApprovalRequestDto input,
            [Service] IApprovalRequestService approvalService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, resultCode, errorMessage) = await approvalService.UpdateAsync(code, input, session);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? $"Fallo al actualizar la solicitud de aprobación #{code} en SAP.");
            }

            return new ApprovalRequestMutationResult
            {
                Success = success,
                Code = resultCode,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Convierte un borrador preliminar aprobado (Draft) en documento real contabilizado en SAP Business One.
        /// </summary>
        public async Task<DraftSaveMutationResult> SaveDraftToDocument(
            int docEntry,
            [Service] IDraftService draftService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, resultDocEntry, generatedDocEntry, errorMessage) = await draftService.SaveDraftToDocumentAsync(docEntry, session);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? $"Fallo al convertir el borrador #{docEntry} en documento real en SAP.");
            }

            return new DraftSaveMutationResult
            {
                Success = success,
                DraftDocEntry = resultDocEntry,
                GeneratedDocEntry = generatedDocEntry,
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

        /// <summary>
        /// Crea un nuevo Usuario en SAP Business One (OUSR).
        /// </summary>
        public async Task<UserMutationResult> CreateUser(
            CreateUserDto input,
            [Service] IUserService userService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, internalKey, errorMessage) = await userService.CreateAsync(input, session);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? "Error al crear usuario en SAP.");
            }

            return new UserMutationResult
            {
                Success = success,
                InternalKey = internalKey,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Actualiza un Usuario existente en SAP Business One.
        /// </summary>
        public async Task<UserMutationResult> UpdateUser(
            int internalKey,
            UpdateUserDto input,
            [Service] IUserService userService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, resultKey, errorMessage) = await userService.UpdateAsync(internalKey, input, session);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? $"Error al actualizar usuario #{internalKey} en SAP.");
            }

            return new UserMutationResult
            {
                Success = success,
                InternalKey = resultKey,
                ErrorMessage = errorMessage
            };
        }

        /// <summary>
        /// Cambia o restablece la contraseña de un Usuario en SAP Business One.
        /// </summary>
        public async Task<UserMutationResult> ChangeUserPassword(
            int internalKey,
            string newPassword,
            [Service] IUserService userService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ISessionManager sessionManager,
            [Service] IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var session = GraphQLAuthHelper.RequireSession(httpContextAccessor, sessionManager, apiKeyOptions);
            var (success, resultKey, errorMessage) = await userService.ChangePasswordAsync(internalKey, newPassword, session);

            if (!success)
            {
                throw new GraphQLException(errorMessage ?? $"Error al cambiar contraseña del usuario #{internalKey}.");
            }

            return new UserMutationResult
            {
                Success = success,
                InternalKey = resultKey,
                ErrorMessage = errorMessage
            };
        }
    }

    public class UserMutationResult
    {
        public bool Success { get; set; }
        public int InternalKey { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BusinessPartnerMutationResult
    {
        public bool Success { get; set; }
        public string CardCode { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class ApprovalRequestMutationResult
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class DraftSaveMutationResult
    {
        public bool Success { get; set; }
        public int DraftDocEntry { get; set; }
        public int? GeneratedDocEntry { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class AttachmentMutationResult
    {
        public bool Success { get; set; }
        public int AbsoluteEntry { get; set; }
        public string? ErrorMessage { get; set; }
    }
}

