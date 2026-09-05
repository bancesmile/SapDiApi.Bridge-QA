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

                // 4. Soporte para llave maestra X-Api-Key
                if (session == null && httpContext.Request.Headers.TryGetValue(apiKeyOptions.Value.HeaderName, out var apiKeyHeader))
                {
                    var apiKey = apiKeyHeader.ToString();
                    if (apiKeyOptions.Value.IsValidKey(apiKey))
                    {
                        session = new UserSession
                        {
                            SessionId = "api-key-master-session",
                            CompanyDB = "Master-ApiKey",
                            UserName = "system-api-key"
                        };
                    }
                }
            }

            if (session == null)
            {
                throw new GraphQLException("Sesión no válida o ausente. Inicie sesión en '/api/v1/Login' y proporcione la cabecera 'B1SESSION'.");
            }

            sessionManager.RefreshSession(session.SessionId);
            return session;
        }
    }
}
