using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Health;
using SapDiApi.Bridge.Services.Health;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
        {
            _healthService = healthService;
        }

        /// <summary>
        /// Estado del servicio
        /// </summary>
        /// <remarks>
        /// Obtiene el estado de operatividad, tiempo de actividad y diagnóstico general del servicio BridgeSap.
        /// </remarks>
        /// <response code="200">El servicio está activo y operando correctamente.</response>
        [HttpGet]
        [Route("~/api/health")]
        [ProducesResponseType(typeof(ApiResponse<HealthStatusDto>), StatusCodes.Status200OK)]
        public IActionResult GetHealth()
        {
            var status = _healthService.GetHealthStatus();
            return Ok(ApiResponse<HealthStatusDto>.Ok(status, "Servicio BridgeSap activo y respondiendo."));
        }
    }
}
