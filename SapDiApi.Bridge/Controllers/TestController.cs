using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Test;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [B1SessionAuth]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;

        public TestController(ILogger<TestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint protegido para validar que la sesión de SAP Service Layer (B1SESSION) o API Key está autorizada.
        /// </summary>
        /// <response code="200">Autenticación exitosa y acceso concedido.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION ausente o expirada.</response>
        [HttpGet("secure-ping")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult SecurePing()
        {
            var session = HttpContext.Items["UserSession"] as UserSession;

            _logger.LogInformation("Solicitud SecurePing autorizada para usuario: {User}", session?.UserName ?? "Anónimo");

            var payload = new
            {
                Authenticated = true,
                SessionId = session?.SessionId,
                UserName = session?.UserName,
                CompanyDB = session?.CompanyDB,
                ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                Message = "Acceso autorizado. La sesión de SAP es válida.",
                TimestampUtc = DateTime.UtcNow
            };

            return Ok(ApiResponse<object>.Ok(payload, "Autenticación correcta con sesión SAP"));
        }

        /// <summary>
        /// Endpoint protegido tipo Echo para probar el envío de cuerpos JSON bajo sesión activa.
        /// </summary>
        /// <param name="request">Datos enviados por el cliente.</param>
        /// <response code="200">Respuesta con los mismos datos procesados.</response>
        /// <response code="400">Petición inválida.</response>
        /// <response code="401">No autorizado.</response>
        [HttpPost("echo")]
        [ProducesResponseType(typeof(ApiResponse<EchoRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult Echo([FromBody] EchoRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Datos de solicitud inválidos."));
            }

            return Ok(ApiResponse<EchoRequestDto>.Ok(request, "Payload recibido y validado exitosamente bajo sesión SAP."));
        }
    }
}
