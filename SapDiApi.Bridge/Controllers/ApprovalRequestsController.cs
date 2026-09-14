using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.ApprovalRequests;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Services.ApprovalRequests;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [B1SessionAuth]
    public class ApprovalRequestsController : ControllerBase
    {
        private readonly IApprovalRequestService _approvalService;
        private readonly ILogger<ApprovalRequestsController> _logger;

        public ApprovalRequestsController(
            IApprovalRequestService approvalService,
            ILogger<ApprovalRequestsController> logger)
        {
            _approvalService = approvalService;
            _logger = logger;
        }

        /// <summary>
        /// Listar solicitudes de aprobación
        /// </summary>
        /// <remarks>
        /// Obtiene una lista de solicitudes de autorización en SAP Business One con filtros opcionales (por solicitante, autorizador asignado, estado, borrador o rango de fechas).
        /// Compatible directamente con SAP Service Layer: `GET /b1s/v1/ApprovalRequests`.
        /// </remarks>
        /// <response code="200">Lista de solicitudes de aprobación recuperadas exitosamente.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/ApprovalRequests")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApprovalRequestDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll([FromQuery] ApprovalRequestFilterDto filter)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var results = await _approvalService.GetFilteredAsync(filter, session);

            return Ok(ApiResponse<IEnumerable<ApprovalRequestDto>>.Ok(
                results,
                $"Solicitudes de aprobación recuperadas para la sociedad '{session?.CompanyDB ?? "Default"}'"));
        }

        /// <summary>
        /// Consultar solicitud de aprobación por código único
        /// </summary>
        /// <remarks>
        /// Obtiene una solicitud de aprobación específica por su clave primaria (OWDD.WddCode), incluyendo sus líneas de etapas (WDD1) y el historial de decisiones (WDD2).
        /// Compatible con formato Service Layer: `GET /b1s/v1/ApprovalRequests(6106)` o `GET /api/v1/ApprovalRequests/6106`.
        /// </remarks>
        /// <param name="code">Código de la solicitud de aprobación (ej: 6106).</param>
        /// <response code="200">Solicitud de aprobación encontrada.</response>
        /// <response code="404">Solicitud no encontrada en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/ApprovalRequests({code})")]
        [Route("~/api/v1/ApprovalRequests/{code}")]
        [ProducesResponseType(typeof(ApiResponse<ApprovalRequestDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByCode(int code)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var approvalReq = await _approvalService.GetByCodeAsync(code, session);

            if (approvalReq == null)
            {
                return NotFound(ServiceLayerErrorResponse.Create(404, $"La solicitud de aprobación con código '{code}' no existe en SAP."));
            }

            return Ok(ApiResponse<ApprovalRequestDto>.Ok(approvalReq, "Solicitud de aprobación recuperada exitosamente."));
        }

        /// <summary>
        /// Actualizar o decidir solicitud de aprobación (Autorizar / Rechazar)
        /// </summary>
        /// <remarks>
        /// Permite a un usuario autorizador aprobar ('Y'), rechazar ('N') o actualizar una solicitud de aprobación en SAP Business One.
        /// Compatible directamente con el payload de SAP Service Layer: `PATCH /b1s/v1/ApprovalRequests(6106)`.
        /// Incluye registro de decisiones en `ApprovalRequestDecisions` con validación de credenciales y comentarios del aprobador.
        /// </remarks>
        /// <param name="code">Código de la solicitud de aprobación a actualizar.</param>
        /// <param name="dto">Payload con las decisiones, líneas, estado u observaciones.</param>
        /// <response code="200">Solicitud de aprobación actualizada correctamente en SAP.</response>
        /// <response code="400">Error en la validación o rechazo por reglas de autorización de SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPatch]
        [HttpPut]
        [Route("~/api/v1/ApprovalRequests({code})")]
        [Route("~/api/v1/ApprovalRequests/{code}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(int code, [FromBody] UpdateApprovalRequestDto dto)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, resultCode, errorMessage) = await _approvalService.UpdateAsync(code, dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? $"Fallo al actualizar la solicitud de aprobación '{code}' en SAP."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { Code = resultCode },
                $"Solicitud de aprobación '{resultCode}' procesada exitosamente en SAP."));
        }
    }
}
