using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Drafts;
using SapDiApi.Bridge.Services.Drafts;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [B1SessionAuth]
    public class DraftsController : ControllerBase
    {
        private readonly IDraftService _draftService;
        private readonly ILogger<DraftsController> _logger;

        public DraftsController(
            IDraftService draftService,
            ILogger<DraftsController> logger)
        {
            _draftService = draftService;
            _logger = logger;
        }

        /// <summary>
        /// Listar documentos preliminares / borradores
        /// </summary>
        /// <remarks>
        /// Obtiene la lista de documentos preliminares (Drafts / ODRF) con filtros opcionales por socio de negocio, tipo de documento, estado o rango de fechas.
        /// Compatible directamente con SAP Service Layer: `GET /b1s/v1/Drafts`.
        /// </remarks>
        /// <response code="200">Lista de borradores obtenida correctamente.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/Drafts")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<DraftDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll([FromQuery] DraftFilterDto filter)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var results = await _draftService.GetFilteredAsync(filter, session);

            return Ok(ApiResponse<IEnumerable<DraftDto>>.Ok(
                results,
                $"Borradores recuperados para la sociedad '{session?.CompanyDB ?? "Default"}'"));
        }

        /// <summary>
        /// Consultar documento preliminar / borrador por DocEntry
        /// </summary>
        /// <remarks>
        /// Obtiene un documento preliminar completo por su clave interna (DocEntry), incluyendo todas las líneas de artículos/servicios, impuestos, condiciones de pago, extensiones de dirección y campos de usuario (UDFs).
        /// Compatible con formato Service Layer: `GET /b1s/v1/Drafts(5137)` o `GET /api/v1/Drafts/5137`.
        /// </remarks>
        /// <param name="docEntry">Identificador interno del borrador (ODRF.DocEntry).</param>
        /// <response code="200">Documento preliminar encontrado.</response>
        /// <response code="404">No encontrado en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/Drafts({docEntry})")]
        [Route("~/api/v1/Drafts/{docEntry}")]
        [ProducesResponseType(typeof(ApiResponse<DraftDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByDocEntry(int docEntry)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var draft = await _draftService.GetByDocEntryAsync(docEntry, session);

            if (draft == null)
            {
                return NotFound(ServiceLayerErrorResponse.Create(404, $"El documento preliminar con DocEntry '{docEntry}' no existe en SAP."));
            }

            return Ok(ApiResponse<DraftDto>.Ok(draft, "Documento preliminar recuperado exitosamente."));
        }

        /// <summary>
        /// Convertir y guardar borrador como documento final definitivo
        /// </summary>
        /// <remarks>
        /// Convierte un documento preliminar aprobado (Draft) en un documento real y contabilizado en SAP Business One (ej: Orden de Compra o Factura final).
        /// Compatible con Service Layer: `POST /b1s/v1/Drafts(5137)/SaveDraftToDocument`.
        /// </remarks>
        /// <param name="docEntry">Clave interna del borrador a convertir.</param>
        /// <response code="200">Borrador convertido exitosamente a documento final en SAP.</response>
        /// <response code="400">Error en la contabilización de SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPost]
        [Route("~/api/v1/Drafts({docEntry})/SaveDraftToDocument")]
        [Route("~/api/v1/Drafts/{docEntry}/SaveDraftToDocument")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SaveDraftToDocument(int docEntry)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, resultDocEntry, generatedDocEntry, errorMessage) = await _draftService.SaveDraftToDocumentAsync(docEntry, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? $"Fallo al convertir el borrador #{docEntry} en documento real en SAP."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { DraftDocEntry = resultDocEntry, GeneratedDocEntry = generatedDocEntry },
                $"Borrador #{resultDocEntry} convertido exitosamente a documento #{generatedDocEntry} en SAP."));
        }
    }
}
