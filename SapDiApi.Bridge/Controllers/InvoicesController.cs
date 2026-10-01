using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SapDiApi.Bridge.Infrastructure.Security;
using SapDiApi.Bridge.Models.Auth;
using SapDiApi.Bridge.Models.Common;
using SapDiApi.Bridge.Models.Invoices;
using SapDiApi.Bridge.Services.ApprovalRequests;
using SapDiApi.Bridge.Services.Companies;
using SapDiApi.Bridge.Services.Invoices;

namespace SapDiApi.Bridge.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [ApiKeyAuth]

    public class InvoicesController : Controller
    {
        private readonly IApprovalRequestService _approvalService;
        private readonly ILogger<InvoicesController> _logger;
        private readonly IFacturaDeudorService _facturaService;
        private readonly ICompanyResolverService _companyResolver;
        public InvoicesController(
            IApprovalRequestService approvalService,
            ILogger<InvoicesController> logger,
            IFacturaDeudorService facturaService,
            ICompanyResolverService companyResolver)
        {
            _approvalService = approvalService;
            _logger = logger;
            _facturaService = facturaService;
            _companyResolver = companyResolver; 
        }


        /// <summary>
        /// Crear Factura de deudores (Invoice) en SAP Business One
        /// </summary>
        /// <remarks>
        /// Crea una nueva factura de deudores (Invoice) en SAP Business One.
        /// Compatible directamente con SAP Service Layer: `POST /b1s/v1/Invoices`.
        /// </remarks>
        /// <response code="200">Factura creada exitosamente.</response>
        /// <response code="401">No autorizado - Sesión B1SESSION requerida.</response>
        /// 
        [HttpPost]
        [Route("~/api/v1/Invoices")]
        [ProducesResponseType(typeof(ApiResponse<FacturaDeudoresDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ServiceLayerErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Crear(
            [FromBody] List<FacturaDeudoresDto> requests)
        {
            int exitosas = 0;
            int fallidas = 0;
            string companyDb = string.Empty;
            var session = HttpContext.Items["ApiClient"] as ApiClientConfig;
            string idEmpresa = Request.Headers["X-Company-Id"].ToString();
            var companyResult = _companyResolver.ResolveCompany(idEmpresa);
            var resultados = new List<object>();
            if (!companyResult.Found)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"No se encontró la empresa {idEmpresa}."
                });
            }

            companyDb = companyResult.SapDatabase;
            
            foreach (var request in requests)
            {
                var resultado =
                    await _facturaService.CrearFacturaDeudoresAsync(
                        request,
                        session,
                        companyDb
                    );

                if (resultado.success)
                    exitosas++;
                else
                    fallidas++;

                resultados.Add(new
                {
                    success = resultado.success,
                    message = resultado.message,
                    docEntry = resultado.docEntry,
                    docNum = resultado.docNum,
                    cardCode = resultado.cardCode,
                    nit = resultado.nit,
                    cardName = resultado.CardName
                });
            }

            return Ok(new
            {
                success = fallidas == 0,
                total = requests.Count,
                exitosas = exitosas,
                fallidas = fallidas,
                resultados = resultados
            });
        }
    }
}