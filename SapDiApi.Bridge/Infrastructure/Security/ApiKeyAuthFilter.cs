using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using SapDiApi.Bridge.Models.Common;

namespace SapDiApi.Bridge.Infrastructure.Security
{
    public class ApiKeyAuthFilter : IAsyncAuthorizationFilter
    {
        private readonly ApiKeyOptions _options;
        private readonly ILogger<ApiKeyAuthFilter> _logger;

        public ApiKeyAuthFilter(IOptions<ApiKeyOptions> options, ILogger<ApiKeyAuthFilter> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            string? extractedApiKey = null;

            // 1. Buscar en el encabezado principal configurado (ej: X-Api-Key)
            if (httpContext.Request.Headers.TryGetValue(_options.HeaderName, out var headerValue))
            {
                extractedApiKey = headerValue.FirstOrDefault();
            }

            // 2. Si no viene en el encabezado principal, buscar en encabezado Authorization (ApiKey <key> o Bearer <key>)
            if (string.IsNullOrWhiteSpace(extractedApiKey) && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authStr = authHeader.ToString();
                if (authStr.StartsWith("ApiKey ", StringComparison.OrdinalIgnoreCase))
                {
                    extractedApiKey = authStr.Substring("ApiKey ".Length).Trim();
                }
                else if (authStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    extractedApiKey = authStr.Substring("Bearer ".Length).Trim();
                }
            }

            // 3. Validar si la llave fue provista y si es válida
            if (string.IsNullOrWhiteSpace(extractedApiKey) || !_options.IsValidKey(extractedApiKey))
            {
                _logger.LogWarning("Intento de acceso no autorizado a {Path} desde {IP}. Llave recibida: {HasKey}",
                    httpContext.Request.Path,
                    httpContext.Connection.RemoteIpAddress,
                    string.IsNullOrEmpty(extractedApiKey) ? "Ausente" : "Inválida");

                var errorPayload = new ApiErrorResponse
                {
                    Success = false,
                    Error = "No autorizado",
                    Details = string.IsNullOrEmpty(extractedApiKey)
                        ? $"Se requiere una llave de acceso válida en el encabezado '{_options.HeaderName}' o 'Authorization: ApiKey <llave>'."
                        : "La llave de acceso provista es inválida o no cuenta con permisos.",
                    StatusCode = StatusCodes.Status401Unauthorized,
                    TimestampUtc = DateTime.UtcNow
                };

                context.Result = new ObjectResult(errorPayload)
                {
                    StatusCode = StatusCodes.Status401Unauthorized
                };

                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}
