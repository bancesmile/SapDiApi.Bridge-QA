using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Services.Auth;

namespace SapDiApi.Bridge.Infrastructure.Security
{
    public class B1SessionAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly ISessionManager _sessionManager;
        private readonly ApiKeyOptions _apiKeyOptions;
        private readonly ILogger<B1SessionAuthFilter> _logger;

        public B1SessionAuthFilter(
            ISessionManager sessionManager,
            IOptions<ApiKeyOptions> apiKeyOptions,
            ILogger<B1SessionAuthFilter> logger)
        {
            _sessionManager = sessionManager;
            _apiKeyOptions = apiKeyOptions.Value;
            _logger = logger;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            string? token = null;

            // 1. Buscar en Cabecera B1SESSION (estándar Service Layer)
            if (httpContext.Request.Headers.TryGetValue("B1SESSION", out var b1Header))
            {
                token = b1Header.FirstOrDefault();
            }

            // 2. Buscar en Cookie B1SESSION
            if (string.IsNullOrWhiteSpace(token) && httpContext.Request.Cookies.TryGetValue("B1SESSION", out var b1Cookie))
            {
                token = b1Cookie;
            }

            // 3. Buscar en Cabecera Authorization: Bearer <token> o B1SESSION <token>
            if (string.IsNullOrWhiteSpace(token) && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authStr = authHeader.ToString();
                if (authStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authStr.Substring("Bearer ".Length).Trim();
                }
                else if (authStr.StartsWith("B1SESSION ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authStr.Substring("B1SESSION ".Length).Trim();
                }
            }

            // Validar sesión contra el SessionManager
            if (!string.IsNullOrWhiteSpace(token))
            {
                var session = _sessionManager.GetSession(token);
                if (session != null)
                {
                    // Refrescar tiempo de inactividad de la sesión (sliding expiration)
                    _sessionManager.RefreshSession(token);

                    // Almacenar datos de la sesión para el controlador
                    httpContext.Items["UserSession"] = session;
                    return Task.CompletedTask;
                }
            }

            // 4. Soporte opcional de llave maestra X-Api-Key si está provista
            if (httpContext.Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var apiKeyHeader))
            {
                var apiKey = apiKeyHeader.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(apiKey) && _apiKeyOptions.IsValidKey(apiKey))
                {
                    httpContext.Items["UserSession"] = new UserSession
                    {
                        SessionId = "api-key-master-session",
                        CompanyDB = "Master-ApiKey",
                        UserName = "system-api-key"
                    };
                    return Task.CompletedTask;
                }
            }

            // Si no hay sesión válida ni llave de acceso
            _logger.LogWarning("Acceso no autorizado rechazado en {Path} desde {IP}. Token: {TokenState}",
                httpContext.Request.Path,
                httpContext.Connection.RemoteIpAddress,
                string.IsNullOrEmpty(token) ? "Ausente" : "Invalido/Expirado");

            var errorPayload = ServiceLayerErrorResponse.Create(
                code: 301,
                message: string.IsNullOrEmpty(token)
                    ? "Acceso no autorizado. Se requiere iniciar sesión en '/api/v1/Login' y enviar la cabecera 'B1SESSION' o Cookie."
                    : "Sesión inválida o expirada por inactividad. Por favor inicie sesión nuevamente."
            );

            context.Result = new ObjectResult(errorPayload)
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            return Task.CompletedTask;
        }
    }
}
