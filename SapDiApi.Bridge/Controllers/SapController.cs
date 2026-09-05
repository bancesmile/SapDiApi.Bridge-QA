using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiKeyAuth]
    public class SapController : ControllerBase
    {
        private readonly ISapDiApiConnector _sapConnector;
        private readonly ILogger<SapController> _logger;

        public SapController(ISapDiApiConnector sapConnector, ILogger<SapController> logger)
        {
            _sapConnector = sapConnector;
            _logger = logger;
        }

        /// <summary>
        /// Probar conexión SAP
        /// </summary>
        /// <remarks>
        /// Prueba la conectividad directa con SAP Business One DI API utilizando las credenciales enviadas y devuelve el diagnóstico exacto de conexión.
        /// </remarks>
        /// <param name="request">Credenciales y sociedad a probar.</param>
        /// <response code="200">Conexión a SAP exitosa.</response>
        /// <response code="400">Error de conexión a SAP (código y descripción nativa de SAP).</response>
        [HttpPost("TestConnection")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public IActionResult TestSapConnection([FromBody] LoginRequestDto request)
        {
            _logger.LogInformation("Ejecutando prueba de conexión SAP DI API para DB {DB} y Usuario {User}",
                request.CompanyDB, request.UserName);

            var session = new UserSession
            {
                CompanyDB = request.CompanyDB,
                UserName = request.UserName
            };

            var (connected, errorMessage) = _sapConnector.Connect(session, request.Password);

            if (connected)
            {
                var result = new
                {
                    Connected = true,
                    CompanyDB = request.CompanyDB,
                    UserName = request.UserName,
                    Status = "Conexión a SAP DI API exitosa.",
                    TimestampUtc = DateTime.UtcNow
                };

                return Ok(ApiResponse<object>.Ok(result, "Conexión establecida con SAP Business One."));
            }

            var errorResult = new
            {
                Connected = false,
                CompanyDB = request.CompanyDB,
                UserName = request.UserName,
                SapError = errorMessage,
                TimestampUtc = DateTime.UtcNow
            };

            return BadRequest(ApiResponse<object>.Fail($"Fallo de conexión SAP: {errorMessage}", errorResult));
        }
    }
}
