using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.BusinessPartners;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Services.BusinessPartners;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [B1SessionAuth]
    public class BusinessPartnersController : ControllerBase
    {
        private readonly IBusinessPartnerService _bpService;
        private readonly ILogger<BusinessPartnersController> _logger;

        public BusinessPartnersController(
            IBusinessPartnerService bpService,
            ILogger<BusinessPartnersController> logger)
        {
            _bpService = bpService;
            _logger = logger;
        }

        /// <summary>
        /// Listar socios de negocio
        /// </summary>
        /// <remarks>
        /// Obtiene la lista de Socios de Negocio en SAP con filtros opcionales (tipo de socio, saldo y texto de búsqueda).
        /// </remarks>
        /// <response code="200">Lista de socios de negocio obtenida correctamente.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/BusinessPartners")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<BusinessPartnerDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll([FromQuery] BusinessPartnerFilterDto filter)
        {
            var session = HttpContext.Items["UserSession"] as UserSession;
            var results = await _bpService.GetFilteredAsync(filter, session);

            return Ok(ApiResponse<IEnumerable<BusinessPartnerDto>>.Ok(
                results,
                $"Socios de negocio recuperados para la sociedad '{session?.CompanyDB ?? "Default"}'"));
        }

        /// <summary>
        /// Consultar socio por código
        /// </summary>
        /// <remarks>
        /// Obtiene un Socio de Negocio específico por su CardCode con todas sus direcciones, contactos, cuentas bancarias y campos de usuario (UDFs) dinámicos.
        /// Compatible con formato Service Layer / OData (ej: `BusinessPartners('P000240')` o `BusinessPartners/P000240`).
        /// </remarks>
        /// <param name="cardCode">Código del Socio de Negocio (ej: P000240, C00001).</param>
        /// <response code="200">Socio de negocio encontrado.</response>
        /// <response code="404">No encontrado en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpGet]
        [Route("~/api/v1/BusinessPartners('{cardCode}')")]
        [ProducesResponseType(typeof(ApiResponse<BusinessPartnerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByCardCode(string cardCode)
        {
            var cleanCardCode = cardCode.Trim('\'', '\"');
            var session = HttpContext.Items["UserSession"] as UserSession;

            var bp = await _bpService.GetByCardCodeAsync(cleanCardCode, session);
            if (bp == null)
            {
                return NotFound(ServiceLayerErrorResponse.Create(404, $"El Socio de Negocio con código '{cleanCardCode}' no existe en SAP."));
            }

            return Ok(ApiResponse<BusinessPartnerDto>.Ok(bp, "Socio de negocio recuperado exitosamente."));
        }

        /// <summary>
        /// Crear socio de negocio
        /// </summary>
        /// <remarks>
        /// Crea un nuevo Socio de Negocio (Proveedor / Cliente / Lead) en SAP Business One.
        /// Compatible directamente con el formato y payload JSON de SAP Service Layer (incluyendo direcciones, contactos, cuentas bancarias, propiedades 1-64 y UDFs).
        /// </remarks>
        /// <param name="dto">Datos completos del socio de negocio.</param>
        /// <response code="201">Socio de negocio creado exitosamente en SAP.</response>
        /// <response code="400">Error de validación o rechazo por reglas contables/negocio en SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPost]
        [Route("~/api/v1/BusinessPartners")]
        [ProducesResponseType(typeof(ApiResponse<BusinessPartnerDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromBody] BusinessPartnerDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CardName))
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "El nombre del socio de negocio (CardName) es obligatorio."));
            }

            var session = HttpContext.Items["UserSession"] as UserSession;
            var (success, createdCardCode, errorMessage) = await _bpService.CreateAsync(dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? "Fallo al crear socio de negocio en SAP."));
            }

            dto.CardCode = createdCardCode;

            return CreatedAtAction(
                nameof(GetByCardCode),
                new { cardCode = createdCardCode },
                ApiResponse<BusinessPartnerDto>.Ok(dto, $"Socio de negocio '{createdCardCode}' creado exitosamente en SAP."));
        }

        /// <summary>
        /// Actualizar socio de negocio
        /// </summary>
        /// <remarks>
        /// Actualiza un Socio de Negocio existente en SAP Business One.
        /// Compatible directamente con el formato PATCH / PUT de SAP Service Layer (sincroniza direcciones, contactos, cuentas bancarias y UDFs).
        /// </remarks>
        /// <param name="cardCode">Código del socio de negocio a actualizar (ej: P000578).</param>
        /// <param name="dto">Campos y colecciones actualizadas.</param>
        /// <response code="200">Socio de negocio actualizado exitosamente en SAP.</response>
        /// <response code="400">Error en la actualización de SAP.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        [HttpPatch]
        [HttpPut]
        [Route("~/api/v1/BusinessPartners('{cardCode}')")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Update(string cardCode, [FromBody] BusinessPartnerDto dto)
        {
            var cleanCardCode = cardCode.Trim('\'', '\"');
            var session = HttpContext.Items["UserSession"] as UserSession;

            var (success, updatedCode, errorMessage) = await _bpService.UpdateAsync(cleanCardCode, dto, session);

            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, errorMessage ?? $"Fallo al actualizar el socio de negocio '{cleanCardCode}' en SAP."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { CardCode = updatedCode },
                $"Socio de negocio '{updatedCode}' actualizado exitosamente en SAP."));
        }
    }
}
