using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Companies;
using SapDiApi.Bridge.Services.Companies;

namespace SapDiApi.Bridge.Controllers
{
    /// <summary>
    /// Catálogo Maestro y Gestión de Sociedades SAP Business One
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiKeyAuth]
    [Produces("application/json")]
    public class CompaniesController : ControllerBase
    {
        private readonly ICompanyResolverService _companyResolver;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(
            ICompanyResolverService companyResolver,
            ILogger<CompaniesController> logger)
        {
            _companyResolver = companyResolver;
            _logger = logger;
        }

        /// <summary>
        /// Listar empresas disponibles para clientes y frontend
        /// </summary>
        /// <remarks>
        /// Retorna el catálogo público de empresas registradas (con CompanyId y CompanyCode)
        /// sin exponer nombres internos de esquemas de bases de datos.
        /// Endpoint: `GET /api/v1/Companies`.
        /// </remarks>
        /// <param name="onlyActive">Filtrar solo empresas activas (por defecto true).</param>
        /// <response code="200">Listado de empresas obtenido exitosamente.</response>
        /// <response code="401">No autorizado - ApiKey inválida o ausente.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<PublicCompanyDto>>), StatusCodes.Status200OK)]
        public IActionResult GetCompanies([FromQuery] bool onlyActive = true)
        {
            var list = _companyResolver.GetPublicCompanies(onlyActive);
            return Ok(ApiResponse<IEnumerable<PublicCompanyDto>>.Ok(
                list,
                $"Se obtuvieron {list.Count()} sociedades SAP disponibles."));
        }

        /// <summary>
        /// Obtener detalle público de una empresa por ID o Código
        /// </summary>
        /// <remarks>
        /// Endpoint: `GET /api/v1/Companies/1` o `GET /api/v1/Companies/DIST_NORTE`.
        /// </remarks>
        /// <param name="identifier">ID numérico (ej: 1) o Código amigable (ej: DIST_NORTE).</param>
        /// <response code="200">Empresa encontrada.</response>
        /// <response code="404">Empresa no encontrada.</response>
        [HttpGet("{identifier}")]
        [ProducesResponseType(typeof(ApiResponse<PublicCompanyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        public IActionResult GetCompanyByIdentifier(string identifier)
        {
            var (found, company, _) = _companyResolver.ResolveCompany(identifier);
            if (!found || company == null)
            {
                return NotFound(ServiceLayerErrorResponse.Create(404, $"La sociedad con identificador '{identifier}' no fue encontrada en el catálogo."));
            }

            var publicDto = new PublicCompanyDto
            {
                CompanyId = company.CompanyId,
                CompanyCode = company.CompanyCode,
                CompanyName = company.CompanyName,
                Localization = company.Localization,
                IsActive = company.IsActive
            };

            return Ok(ApiResponse<PublicCompanyDto>.Ok(publicDto, $"Sociedad '{company.CompanyCode}' encontrada."));
        }

        /// <summary>
        /// Sincronizar catálogo de empresas desde SAP (SBOCOMMON.SRGC)
        /// </summary>
        /// <remarks>
        /// Consulta la tabla maestra `SBOCOMMON.SRGC` en SAP, inserta nuevas bases de datos detectadas
        /// y actualiza nombres o estados de las existentes en el almacenamiento local SQLite.
        /// Endpoint: `POST /api/v1/Companies/sync`.
        /// </remarks>
        /// <response code="200">Sincronización completada exitosamente.</response>
        /// <response code="400">Error durante la sincronización.</response>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SyncFromSap()
        {
            try
            {
                var (total, added, updated) = await _companyResolver.SyncFromSapAsync();
                return Ok(ApiResponse<object>.Ok(
                    new { TotalProcessed = total, NewAdded = added, Updated = updated },
                    $"Sincronización finalizada con éxito: {total} procesadas, {added} agregadas, {updated} actualizadas."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al sincronizar empresas desde SBOCOMMON.SRGC.");
                return BadRequest(ServiceLayerErrorResponse.Create(400, $"Error al sincronizar con SAP: {ex.Message}"));
            }
        }

        /// <summary>
        /// Recargar la caché en memoria RAM desde el archivo local SQLite
        /// </summary>
        /// <remarks>
        /// Permite recargar en caliente el catálogo de empresas en memoria (0 ms de latencia)
        /// sin necesidad de reiniciar el servicio.
        /// Endpoint: `POST /api/v1/Companies/reload`.
        /// </remarks>
        [HttpPost("reload")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReloadCache()
        {
            await _companyResolver.ReloadCacheAsync();
            var count = _companyResolver.GetAll(false).Count();
            return Ok(ApiResponse<object>.Ok(
                new { TotalCached = count },
                $"Caché en memoria recargada exitosamente. Total empresas en RAM: {count}."));
        }

        /// <summary>
        /// Importar masivamente empresas en el catálogo local SQLite
        /// </summary>
        /// <remarks>
        /// Permite cargar o actualizar un listado de empresas directamente en el catálogo local SQLite
        /// y recargar inmediatamente la memoria RAM.
        /// Endpoint: `POST /api/v1/Companies/import`.
        /// </remarks>
        [HttpPost("import")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ImportCompanies([FromBody] List<CompanyDto> companies)
        {
            if (companies == null || companies.Count == 0)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "El listado de empresas a importar no puede estar vacío."));
            }

            int count = 0;
            foreach (var comp in companies)
            {
                if (await _companyResolver.UpsertCompanyAsync(comp))
                {
                    count++;
                }
            }

            return Ok(ApiResponse<object>.Ok(
                new { TotalImported = count },
                $"Se importaron/actualizaron {count} empresas exitosamente en el catálogo local."));
        }

        /// <summary>
        /// Registrar o actualizar una sociedad en el catálogo local
        /// </summary>
        /// <remarks>
        /// Endpoint: `POST /api/v1/Companies`.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertCompany([FromBody] CompanyDto company)
        {
            if (string.IsNullOrWhiteSpace(company.SapDatabase))
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "El campo 'SapDatabase' es obligatorio."));
            }

            if (string.IsNullOrWhiteSpace(company.CompanyCode))
            {
                company.CompanyCode = company.SapDatabase.Replace("SBO_", "").ToUpperInvariant();
            }

            if (string.IsNullOrWhiteSpace(company.CompanyName))
            {
                company.CompanyName = company.CompanyCode;
            }

            var success = await _companyResolver.UpsertCompanyAsync(company);
            if (!success)
            {
                return BadRequest(ServiceLayerErrorResponse.Create(400, "Fallo al registrar la empresa en el catálogo."));
            }

            return Ok(ApiResponse<object>.Ok(
                new { company.CompanyCode, company.SapDatabase },
                $"Sociedad '{company.CompanyCode}' ({company.SapDatabase}) registrada/actualizada exitosamente."));
        }
    }
}
