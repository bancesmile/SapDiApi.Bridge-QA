using HotChocolate;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Services.Auth;

namespace SapDiApi.Bridge.GraphQL
{
    public static class GraphQLAuthHelper
    {
        /// <summary>
        /// Extrae y valida la sesión de usuario desde las cabeceras HTTP (B1SESSION, Cookie o X-Api-Key).
        /// </summary>
        public static UserSession RequireSession(
            IHttpContextAccessor httpContextAccessor,
            ISessionManager sessionManager,
            IOptions<ApiKeyOptions> apiKeyOptions)
        {
            var httpContext = httpContextAccessor.HttpContext;
            UserSession? session = null;

            if (httpContext != null)
            {
                // 1. Buscar token en cabecera B1SESSION
                if (httpContext.Request.Headers.TryGetValue("B1SESSION", out var b1Header))
                {
                    session = sessionManager.GetSession(b1Header.ToString());
                }
                // 2. Buscar en Cookie B1SESSION
                else if (httpContext.Request.Cookies.TryGetValue("B1SESSION", out var b1Cookie))
                {
                    session = sessionManager.GetSession(b1Cookie);
                }

                // 3. Soporte para cabecera Authorization: Bearer o B1SESSION
                if (session == null && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    var authStr = authHeader.ToString();
                    if (authStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        session = sessionManager.GetSession(authStr.Substring("Bearer ".Length).Trim());
                    }
                    else if (authStr.StartsWith("B1SESSION ", StringComparison.OrdinalIgnoreCase))
                    {
                        session = sessionManager.GetSession(authStr.Substring("B1SESSION ".Length).Trim());
                    }
                }

                // 4. Soporte para llave maestra X-Api-Key (Service Account / Multi-empresa)
                if (session == null)
                {
                    string? apiKey = null;
                    if (httpContext.Request.Headers.TryGetValue(apiKeyOptions.Value.HeaderName, out var apiKeyHeader))
                    {
                        apiKey = apiKeyHeader.ToString();
                    }
                    else if (httpContext.Request.Headers.TryGetValue("Authorization", out var authApiKeyHeader))
                    {
                        var str = authApiKeyHeader.ToString();
                        if (str.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                        {
                            apiKey = str.Substring("ApiKey ".Length).Trim();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        var client = apiKeyOptions.Value.GetClient(apiKey);
                        if (client != null)
                        {
                            var config = httpContext.RequestServices.GetService<IConfiguration>();

                            // Resolver CompanyDB dinámico
                            string? companyDb = null;
                            if (httpContext.Request.Headers.TryGetValue("X-Company-DB", out var dbHeader) ||
                                httpContext.Request.Headers.TryGetValue("CompanyDB", out dbHeader) ||
                                httpContext.Request.Headers.TryGetValue("X-CompanyDB", out dbHeader))
                            {
                                companyDb = dbHeader.FirstOrDefault();
                            }

                            if (string.IsNullOrWhiteSpace(companyDb) &&
                                (httpContext.Request.Query.TryGetValue("companyDB", out var queryDb) ||
                                 httpContext.Request.Query.TryGetValue("CompanyDB", out queryDb) ||
                                 httpContext.Request.Query.TryGetValue("company_db", out queryDb)))
                            {
                                companyDb = queryDb.FirstOrDefault();
                            }

                            if (string.IsNullOrWhiteSpace(companyDb))
                            {
                                companyDb = config?["SapSettings:DefaultCompanyDB"] ?? string.Empty;
                            }

                            if (!apiKeyOptions.Value.IsCompanyAllowed(client, companyDb))
                            {
                                throw new GraphQLException($"La aplicación '{client.Name}' no cuenta con permisos para operar en la sociedad SAP '{companyDb}'.");
                            }

                            var serviceUser = config?["SapSettings:ServiceUserName"]
                                ?? config?["SapSettings:DefaultUserName"]
                                ?? "manager";

                            var servicePassword = config?["SapSettings:ServicePassword"]
                                ?? config?["SapSettings:DefaultPassword"]
                                ?? string.Empty;

                            var auditUser = httpContext.Request.Headers["X-Audit-User"].FirstOrDefault()
                                ?? httpContext.Request.Headers["AuditUser"].FirstOrDefault()
                                ?? "graphql-portal-user";

                            var customApp = httpContext.Request.Headers["X-Audit-App"].FirstOrDefault()
                                ?? httpContext.Request.Headers["AuditApp"].FirstOrDefault();

                            var auditApp = !string.IsNullOrWhiteSpace(customApp)
                                ? $"{client.Name} ({customApp})"
                                : client.Name;

                            session = new UserSession
                            {
                                SessionId = $"api-key-{client.Id}-{Guid.NewGuid():N}",
                                CompanyDB = companyDb,
                                UserName = serviceUser,
                                Password = servicePassword,
                                AuditUser = auditUser,
                                AuditApp = auditApp,
                                ExecutionMode = "ServicePool"
                            };
                        }
                    }
                }
            }

            if (session == null)
            {
                throw new GraphQLException("Sesión no válida o ausente. Inicie sesión en '/api/v1/Login' o proporcione 'X-Api-Key'.");
            }

            if (session.ExecutionMode != "ServicePool")
            {
                sessionManager.RefreshSession(session.SessionId);
            }

            return session;
        }
    }
}
