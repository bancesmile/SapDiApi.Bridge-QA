using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Services.Auth;
using SapDiApi.Bridge.Services.Companies;

namespace SapDiApi.Bridge.Infrastructure.Security
{
    public class B1SessionAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly ISessionManager _sessionManager;
        private readonly ApiKeyOptions _apiKeyOptions;
        private readonly ICompanyResolverService _companyResolver;
        private readonly IConfiguration _configuration;
        private readonly ILogger<B1SessionAuthFilter> _logger;

        public B1SessionAuthFilter(
            ISessionManager sessionManager,
            IOptions<ApiKeyOptions> apiKeyOptions,
            ICompanyResolverService companyResolver,
            IConfiguration configuration,
            ILogger<B1SessionAuthFilter> logger)
        {
            _sessionManager = sessionManager;
            _apiKeyOptions = apiKeyOptions.Value;
            _companyResolver = companyResolver;
            _configuration = configuration;
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

            // 3. Buscar en Cabecera Authorization: Bearer <token> o B1SESSION <token> o ApiKey <key>
            string? extractedApiKey = null;
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
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
                else if (authStr.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                {
                    extractedApiKey = authStr.Substring("ApiKey ".Length).Trim();
                }
            }

            // Validar sesión contra el SessionManager si viene token B1SESSION
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

            // 4. Soporte de autenticación por X-Api-Key (Service Account / Single License Multi-Empresa)
            if (string.IsNullOrWhiteSpace(extractedApiKey) && httpContext.Request.Headers.TryGetValue(_apiKeyOptions.HeaderName, out var apiKeyHeader))
            {
                extractedApiKey = apiKeyHeader.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(extractedApiKey))
            {
                var client = _apiKeyOptions.GetClient(extractedApiKey);
                if (client != null)
                {
                    // 1. Extraer identificador de empresa: X-Company-Id -> X-Company-Code -> X-Company-DB -> Query params
                    string? rawCompanyIdentifier = null;

                    if (httpContext.Request.Headers.TryGetValue("X-Company-Id", out var compIdHeader) ||
                        httpContext.Request.Headers.TryGetValue("CompanyId", out compIdHeader) ||
                        httpContext.Request.Headers.TryGetValue("X-Company-ID", out compIdHeader))
                    {
                        rawCompanyIdentifier = compIdHeader.FirstOrDefault();
                    }

                    if (string.IsNullOrWhiteSpace(rawCompanyIdentifier) &&
                        (httpContext.Request.Headers.TryGetValue("X-Company-Code", out var codeHeader) ||
                         httpContext.Request.Headers.TryGetValue("CompanyCode", out codeHeader)))
                    {
                        rawCompanyIdentifier = codeHeader.FirstOrDefault();
                    }

                    if (string.IsNullOrWhiteSpace(rawCompanyIdentifier) &&
                        (httpContext.Request.Headers.TryGetValue("X-Company-DB", out var dbHeader) ||
                         httpContext.Request.Headers.TryGetValue("CompanyDB", out dbHeader) ||
                         httpContext.Request.Headers.TryGetValue("X-CompanyDB", out dbHeader)))
                    {
                        rawCompanyIdentifier = dbHeader.FirstOrDefault();
                    }

                    if (string.IsNullOrWhiteSpace(rawCompanyIdentifier) &&
                        (httpContext.Request.Query.TryGetValue("companyId", out var queryId) ||
                         httpContext.Request.Query.TryGetValue("company_id", out queryId) ||
                         httpContext.Request.Query.TryGetValue("companyCode", out queryId) ||
                         httpContext.Request.Query.TryGetValue("company_code", out queryId) ||
                         httpContext.Request.Query.TryGetValue("companyDB", out queryId) ||
                         httpContext.Request.Query.TryGetValue("company_db", out queryId)))
                    {
                        rawCompanyIdentifier = queryId.FirstOrDefault();
                    }

                    // 2. Resolver a través de ICompanyResolverService (en RAM, 0 ms)
                    var (found, companyDto, resolvedDbName) = _companyResolver.ResolveCompany(rawCompanyIdentifier);
                    string companyDb = resolvedDbName;

                    if (string.IsNullOrWhiteSpace(companyDb))
                    {
                        companyDb = _configuration["SapSettings:DefaultCompanyDB"] ?? string.Empty;
                    }

                    // 3. Validar si el cliente tiene permiso para acceder a esta sociedad
                    if (!_apiKeyOptions.IsCompanyAllowed(client, companyDb, companyDto))
                    {
                        _logger.LogWarning("Acceso prohibido: La aplicación '{ClientName}' ({ClientId}) no tiene permisos sobre la sociedad '{CompanyDB}' (Id/Code: {Identifier}).",
                            client.Name, client.Id, companyDb, rawCompanyIdentifier);

                        var forbiddenPayload = ServiceLayerErrorResponse.Create(
                            code: 403,
                            message: $"La aplicación '{client.Name}' no cuenta con permisos para operar en la sociedad SAP solicitada ('{rawCompanyIdentifier ?? companyDb}')."
                        );

                        context.Result = new ObjectResult(forbiddenPayload)
                        {
                            StatusCode = StatusCodes.Status403Forbidden
                        };

                        return Task.CompletedTask;
                    }

                    if (companyDto != null)
                    {
                        httpContext.Items["Company"] = companyDto;
                    }

                    // Resolver usuario y contraseña de servicio SAP desde appsettings
                    var serviceUser = _configuration["SapSettings:ServiceUserName"]
                        ?? _configuration["SapSettings:DefaultUserName"]
                        ?? "manager";

                    var servicePassword = _configuration["SapSettings:ServicePassword"]
                        ?? _configuration["SapSettings:DefaultPassword"]
                        ?? string.Empty;

                    // Auditoría: Identificar usuario operador y la aplicación cliente
                    var auditUser = httpContext.Request.Headers["X-Audit-User"].FirstOrDefault()
                        ?? httpContext.Request.Headers["AuditUser"].FirstOrDefault()
                        ?? "portal-user";

                    var customApp = httpContext.Request.Headers["X-Audit-App"].FirstOrDefault()
                        ?? httpContext.Request.Headers["AuditApp"].FirstOrDefault();

                    var auditApp = !string.IsNullOrWhiteSpace(customApp)
                        ? $"{client.Name} ({customApp})"
                        : client.Name;

                    httpContext.Items["UserSession"] = new UserSession
                    {
                        SessionId = $"api-key-{client.Id}-{Guid.NewGuid():N}",
                        CompanyDB = companyDb,
                        UserName = serviceUser,
                        Password = servicePassword,
                        AuditUser = auditUser,
                        AuditApp = auditApp,
                        ExecutionMode = "ServicePool"
                    };

                    return Task.CompletedTask;
                }
            }

            // Si no hay sesión válida ni llave de acceso
            _logger.LogWarning("Acceso no autorizado rechazado en {Path} desde {IP}. Token: {TokenState}",
                httpContext.Request.Path,
                httpContext.Connection.RemoteIpAddress,
                string.IsNullOrEmpty(token) && string.IsNullOrEmpty(extractedApiKey) ? "Ausente" : "Invalido/Expirado");

            var errorPayload = ServiceLayerErrorResponse.Create(
                code: 301,
                message: string.IsNullOrEmpty(token) && string.IsNullOrEmpty(extractedApiKey)
                    ? $"Acceso no autorizado. Se requiere iniciar sesión en '/api/v1/Login' con 'B1SESSION' o proporcionar el encabezado '{_apiKeyOptions.HeaderName}'."
                    : "Sesión inválida o llave de acceso no autorizada."
            );

            context.Result = new ObjectResult(errorPayload)
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            return Task.CompletedTask;
        }
    }
}
