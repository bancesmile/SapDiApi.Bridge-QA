using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Attachments;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Services.Sap;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [B1SessionAuth]
    public class AttachmentsController : ControllerBase
    {
        private readonly ISapDiApiConnector _sapConnector;
        private readonly ILogger<AttachmentsController> _logger;

        public AttachmentsController(
            ISapDiApiConnector sapConnector,
            ILogger<AttachmentsController> logger)
        {
            _sapConnector = sapConnector;
            _logger = logger;
        }

        /// <summary>
        /// Crear anexo
        /// </summary>
        /// <remarks>
        /// Crea un nuevo lote de anexos en SAP Business One (Attachments2 / OATC).
        /// Compatible directamente con el formato de SAP Service Layer.
        /// Devuelve el `AbsoluteEntry` generado para asociarlo en `AttachmentEntry` del Socio de Negocio u otro documento.
        /// </remarks>
        /// <param name="dto">Líneas del anexo con ruta física (UNC o local), nombre de archivo y UDFs opcionales.</param>
        /// <response code="201">Anexo creado exitosamente en SAP. Devuelve el AbsoluteEntry generado.</response>
        /// <response code="400">Error al guardar anexo en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPost]
        [Route("~/api/v1/Attachments2")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateAttachment([FromBody] AttachmentDto dto)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            if (session == null)
            {
                return Unauthorized(ServiceLayerErrorResponse.Create(401, "Sesión no válida o expirada."));
            }

            var (success, entry, errorMessage) = await _sapConnector.CreateOrUpdateAttachmentAsync(session, dto);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? "Fallo al crear anexo en SAP."));
            }

            var result = new
            {
                AbsoluteEntry = entry,
                AttachmentEntry = entry,
                Message = $"Anexo #{entry} creado exitosamente en SAP."
            };

            return StatusCode(StatusCodes.Status201Created, ApiResponse<object>.Ok(result, "Anexo registrado en SAP Business One."));
        }

        /// <summary>
        /// Actualizar anexo
        /// </summary>
        /// <remarks>
        /// Actualiza o agrega líneas y archivos a un lote de anexos existente en SAP Business One (Attachments2).
        /// Soporta actualización de rutas físicas y campos de usuario (UDFs).
        /// </remarks>
        /// <param name="id">ID del anexo existente (AbsoluteEntry / AttachmentEntry).</param>
        /// <param name="dto">Nuevas líneas o archivos a actualizar/agregar.</param>
        /// <response code="200">Anexo actualizado exitosamente en SAP.</response>
        /// <response code="400">Error al actualizar anexo en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPatch]
        [HttpPut]
        [Route("~/api/v1/Attachments2({id})")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateAttachment(int id, [FromBody] AttachmentDto dto)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            if (session == null)
            {
                return Unauthorized(ServiceLayerErrorResponse.Create(401, "Sesión no válida o expirada."));
            }

            dto.AbsoluteEntry = id;
            var (success, entry, errorMessage) = await _sapConnector.CreateOrUpdateAttachmentAsync(session, dto);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? "Fallo al actualizar anexo en SAP."));
            }

            var result = new
            {
                AbsoluteEntry = entry,
                AttachmentEntry = entry,
                Message = $"Anexo #{entry} actualizado exitosamente en SAP."
            };

            return Ok(ApiResponse<object>.Ok(result, "Anexo actualizado en SAP Business One."));
        }
    }
}
