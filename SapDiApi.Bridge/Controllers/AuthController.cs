using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Services.Auth;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[action]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly ISessionManager _sessionManager;
        private readonly ISapAuthService _sapAuthService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ISessionManager sessionManager, ISapAuthService sapAuthService, ILogger<AuthController> logger)
        {
            _sessionManager = sessionManager;
            _sapAuthService = sapAuthService;
            _logger = logger;
        }

        /// <summary>
        /// Iniciar sesión
        /// </summary>
        /// <remarks>
        /// Inicia sesión estilo SAP Service Layer validando credenciales de usuario y sociedad.
        /// Soporta modo directo (usuario SAP con licencia) y modo servicio con operador de auditoría (AuditUser).
        /// Devuelve el `SessionId` y establece la cookie `B1SESSION`.
        /// </remarks>
        /// <param name="request">Credenciales: CompanyDB, UserName, Password, y opcionalmente AuditUser y AuditApp.</param>
        /// <response code="200">Inicio de sesión exitoso. Devuelve SessionId y establece la cookie B1SESSION.</response>
        /// <response code="400">Parámetros incompletos.</response>
        /// <response code="401">Credenciales inválidas en SAP.</response>
        [HttpPost]
        [Route("~/api/v1/Login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "Los campos CompanyDB, UserName y Password son requeridos."));
            }

            var (success, errorMessage) = await _sapAuthService.ValidateCredentialsAsync(request);
            if (!success)
            {
                return Unauthorized(ServiceLayerErrorResponse.Create(401, errorMessage ?? "Credenciales de SAP incorrectas."));
            }

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var timeoutMinutes = 30;

            var executionMode = !string.IsNullOrWhiteSpace(request.AuditUser) ? "ServicePool" : "Direct";

            var session = _sessionManager.CreateSession(
                companyDb: request.CompanyDB,
                userName: request.UserName,
                password: request.Password,
                clientIp: clientIp,
                timeoutMinutes: timeoutMinutes,
                auditUser: request.AuditUser,
                auditApp: request.AuditApp,
                executionMode: executionMode);

            Response.Cookies.Append("B1SESSION", session.SessionId, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(timeoutMinutes),
                Path = "/"
            });

            Response.Headers["Set-Cookie"] = $"B1SESSION={session.SessionId}; path=/; HttpOnly; SameSite=Lax";

            var response = new LoginResponseDto
            {
                SessionId = session.SessionId,
                Version = "10.0",
                SessionTimeout = timeoutMinutes
            };

            return Ok(response);
        }

        /// <summary>
        /// Cerrar sesión
        /// </summary>
        /// <remarks>
        /// Cierra la sesión activa de SAP Service Layer liberando recursos y revocando la cookie `B1SESSION`.
        /// </remarks>
        /// <response code="204">Sesión cerrada exitosamente.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPost]
        [Route("~/api/v1/Logout")]
        [B1SessionAuth]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult Logout()
        {
            if (HttpContext.Items["UserSession"] is UserSession session)
            {
                _sessionManager.TerminateSession(session.SessionId);
            }

            Response.Cookies.Delete("B1SESSION", new CookieOptions { Path = "/" });

            return NoContent();
        }

        /// <summary>
        /// Información de sesión
        /// </summary>
        /// <remarks>
        /// Retorna información de diagnóstico de la sesión autenticada actual y su operador de auditoría.
        /// </remarks>
        [HttpGet]
        [Route("~/api/v1/SessionInfo")]
        [B1SessionAuth]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public IActionResult GetSessionInfo()
        {
            if (HttpContext.Items["UserSession"] is UserSession session)
            {
                var remainingMinutes = Math.Max(0, (session.ExpiresAtUtc - DateTime.UtcNow).TotalMinutes);

                var info = new
                {
                    session.SessionId,
                    session.CompanyDB,
                    session.UserName,
                    session.AuditUser,
                    session.AuditApp,
                    session.ExecutionMode,
                    session.CreatedAtUtc,
                    session.LastActivityUtc,
                    session.ExpiresAtUtc,
                    RemainingMinutes = Math.Round(remainingMinutes, 1),
                    session.ClientIp
                };

                return Ok(ApiResponse<object>.Ok(info, "Información de sesión activa recuperada."));
            }

            return Unauthorized(ServiceLayerErrorResponse.Create(401, "No se encontró sesión activa."));
        }
    }
}
