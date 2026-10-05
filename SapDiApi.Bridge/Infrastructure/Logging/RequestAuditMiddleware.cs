using System.Diagnostics;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;

namespace SapDiApi.Bridge.Infrastructure.Logging
{
    /// <summary>
    /// Middleware de auditoría que registra cada petición en archivos de texto con rotación diaria.
    /// Registra: Fecha/Hora, Máquina/Host, IP Cliente, Usuario SAP, Operador Externo (X-Audit-User), Sociedad, Endpoint y Tiempo de respuesta.
    /// </summary>
    public class RequestAuditMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestAuditMiddleware> _logger;

        public RequestAuditMiddleware(RequestDelegate next, ILogger<RequestAuditMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestTime = DateTime.UtcNow;

            // Extraer cabeceras de auditoría externa si existen
            var auditUserHeader = context.Request.Headers["X-Audit-User"].FirstOrDefault();
            var auditAppHeader = context.Request.Headers["X-Audit-App"].FirstOrDefault();

            // Continuar con la petición
            await _next(context);

            stopwatch.Stop();

            var path = context.Request.Path.Value ?? string.Empty;
            if (path.EndsWith(".ico") || path.EndsWith(".css") || path.EndsWith(".js"))
            {
                return;
            }

            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown-IP";
            var clientMachine = context.Request.Headers["User-Agent"].FirstOrDefault() ?? context.Request.Headers["Host"].FirstOrDefault() ?? "Unknown-Client";

            var userSession = context.Items["UserSession"] as UserSession;
            var apiClient = context.Items["ApiClient"] as ApiClientConfig;
            var sapUser = userSession?.UserName ?? "Anonymous";
            var companyDb = userSession?.CompanyDB ?? "N/A";
            var sessionId = userSession?.SessionId ?? "-";
            var operatorUser = !string.IsNullOrWhiteSpace(auditUserHeader) ? auditUserHeader : (userSession?.AuditUser ?? "-");
            var operatorApp = !string.IsNullOrWhiteSpace(auditAppHeader) ? auditAppHeader : (userSession?.AuditApp ?? apiClient?.Name ?? "-");
            var mode = userSession?.ExecutionMode ?? (apiClient != null ? "ApiKey" : "Standard");

            var method = context.Request.Method;
            var fullPath = $"{path}{context.Request.QueryString}";
            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            // Formato estructurado de alta legibilidad para los archivos diarios de auditoría
            _logger.LogInformation(
                "AUDIT | Time: {RequestTime:yyyy-MM-dd HH:mm:ss.fff} | Machine/Host: {ClientMachine} | IP: {ClientIp} | Operator: {OperatorUser} | App: {OperatorApp} | SapUser: {SapUser}@{CompanyDb} | Mode: {Mode} | SessionId: {SessionId} | Method: {Method} | Endpoint: {FullPath} | Status: {StatusCode} | Duration: {ElapsedMs}ms",
                requestTime,
                clientMachine,
                clientIp,
                operatorUser,
                operatorApp,
                sapUser,
                companyDb,
                mode,
                sessionId,
                method,
                fullPath,
                statusCode,
                elapsedMs);
        }
    }

    public static class RequestAuditMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestAuditLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestAuditMiddleware>();
        }
    }
}
